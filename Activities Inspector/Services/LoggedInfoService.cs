using CSharpFunctionalExtensions;
using Activities_Inspector.Constants;
using Activities_Inspector.Models;
using Activities_Inspector.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Activities_Inspector.Services
{
    public class LoggedInfoService : ILoggedInfoService
    {
        private readonly Evidence.IEvidenceSourceProvider _sources;

        public LoggedInfoService(Evidence.IEvidenceSourceProvider sources)
        {
            _sources = sources;
        }

        public IReadOnlyList<IntegrityRecord> LastIntegrityManifest { get; private set; }
            = new List<IntegrityRecord>();

        public LogCoverage LastCoverage { get; private set; }

        public async Task<Result<List<SessionEntry>>> GetSessionsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var manifest = new List<IntegrityRecord>();
                LastIntegrityManifest = manifest;
                LastCoverage = null;

                if (_sources.Current.IsLive)
                {
                    manifest.Add(IntegrityRecord.LiveSource(EntryType.Sessions,
                        "Registro Sicurezza (API live)"));
                }

                var systemEvents = await GetSecurityEventLogEntriesAsync(manifest, cancellationToken);

                if (systemEvents.Count == 0)
                    return Result.Failure<List<SessionEntry>>("Il registro eventi Sicurezza e' vuoto o illeggibile: " +
                        "impossibile distinguere assenza di accessi da log ruotato/cancellato o permessi insufficienti.");

                LastCoverage = LogCoverage.FromTimes(AppConstants.EventLog.SecurityLog, systemEvents.Select(e => e.TimeGenerated));

                // Filtri e pairing su decine di migliaia di eventi: CPU-bound
                // su thread pool, mai sullo UI thread.
                var sessionsList = await Task.Run(() => BuildSessions(systemEvents, cancellationToken), cancellationToken);

                return Result.Success(sessionsList);
            }
            catch (Exception ex) when (!(ex is OperationCanceledException))
            {
                return Result.Failure<List<SessionEntry>>(ex.ToString());
            }
        }

        private Task<List<IEventRecord>> GetSecurityEventLogEntriesAsync(List<IntegrityRecord> manifest, CancellationToken cancellationToken = default)
        {
            if (!_sources.Current.IsLive)
            {
                var path = _sources.Current.GetEventLogPath(AppConstants.EventLog.SecurityLog);
                manifest.Add(IntegrityHasher.HashFile(path, EntryType.Sessions));
                return Task.Run(() => Evidence.EvtxFileReader.ReadEvents(path), cancellationToken);
            }

            return Task.Run(() =>
            {
                using var eventLog = new EventLog { Log = AppConstants.EventLog.SecurityLog };
                var entries = new List<IEventRecord>();

                foreach (EventLogEntry entry in eventLog.Entries)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    entries.Add(new LiveEventRecord(entry));
                }

                return entries;
            }, cancellationToken);
        }

        // ---------------------------------------------------------------------------------------------------
        // Ricostruzione delle sessioni
        // ---------------------------------------------------------------------------------------------------

        /// <summary>Un accesso (o la coppia di sessioni collegate di uno stesso accesso) in attesa di una fine.</summary>
        private sealed class SessionCandidate
        {
            public LogOnEntry Primary;                       // sessione "limitata" (token non elevato) se c'è una coppia
            public string LinkedIndex;                       // Logon ID dell'altra sessione della coppia, se unita
            public HashSet<string> Ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            public DateTime? LogOffTime;
        }

        private enum LogoffKind
        {
            UserInitiated,   // 4647
            SessionEnded     // 4634
        }

        private sealed class LogoffRecord
        {
            public string Index;
            public DateTime Time;
            public string AccountName;
            public string DomainName;
            public string MachineName;
            public LogoffKind Kind;
        }

        /// <summary>
        /// Sessioni a partire dagli eventi del registro Sicurezza.
        /// - Solo accessi guidati da persona (2 interattivo, 9 nuove credenziali, 10 remoto, 11 in cache). Lo sblocco (7)
        ///   non e' una sessione ed e' escluso, come in WinLogOnView.
        /// - Le due sessioni collegate di un accesso con token elevato (stesso 4624 duplicato, Logon ID diversi) sono
        ///   unite in una sola riga.
        /// - La fine e' l'evento 4647 (uscita avviata dall'utente) o, in mancanza, 4634 (sessione chiusa), su uno
        ///   qualsiasi dei due Logon ID. Si abbina all'accesso piu' recente con quell'ID precedente alla fine: i Logon ID
        ///   ripartono a ogni avvio e possono ripetersi.
        /// - Un 4634 senza accesso corrispondente e' ignorato (sono migliaia: servizi, batch, rete); un 4647 senza
        ///   accesso produce una riga con nota.
        /// </summary>
        internal static List<SessionEntry> BuildSessions(IReadOnlyList<IEventRecord> events, CancellationToken cancellationToken = default)
        {
            var candidates = MergeLinkedSessions(GetLogOnEntries(events));
            var logoffs = GetLogoffRecords(events)
                .OrderBy(l => l.Time)
                .ThenBy(l => l.Kind == LogoffKind.UserInitiated ? 0 : 1)
                .ToList();

            var orphans = new List<SessionEntry>();

            foreach (var logoff in logoffs)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var candidate = candidates
                    .Where(c => c.LogOffTime == null && c.Ids.Contains(logoff.Index) && c.Primary.TimeGenerated <= logoff.Time)
                    .OrderByDescending(c => c.Primary.TimeGenerated)
                    .FirstOrDefault();

                if (candidate != null)
                {
                    candidate.LogOffTime = logoff.Time;
                    continue;
                }

                // Nessun accesso aperto con quell'ID: un 4634 e' normale (sessioni di servizio/rete non elencate, o
                // gia' chiusa dal 4647); un 4647 e' un'uscita il cui accesso non e' nel log
                var alreadyClosed = candidates.Any(c => c.LogOffTime != null && c.Ids.Contains(logoff.Index)
                    && c.Primary.TimeGenerated <= logoff.Time);

                if (logoff.Kind == LogoffKind.UserInitiated && !alreadyClosed)
                {
                    orphans.Add(new SessionEntry(
                        index: logoff.Index,
                        userName: logoff.AccountName,
                        group: logoff.DomainName,
                        machineName: logoff.MachineName,
                        logOnTime: DateTime.MinValue,
                        logOffTime: logoff.Time,
                        duration: null,
                        networdAddress: string.Empty,
                        accessType: string.Empty)
                    {
                        Note = "Accesso non trovato nel log"
                    });
                }
            }

            var list = new List<SessionEntry>(candidates.Count + orphans.Count);

            foreach (var c in candidates)
            {
                var logon = c.Primary;
                list.Add(new SessionEntry(
                    index: c.LinkedIndex == null ? logon.Index : $"{logon.Index} (collegato {c.LinkedIndex})",
                    userName: logon.AccountName,
                    group: logon.DomainName,
                    machineName: logon.MachinName,
                    logOnTime: logon.TimeGenerated,
                    logOffTime: c.LogOffTime,
                    duration: c.LogOffTime.HasValue ? c.LogOffTime.Value.Subtract(logon.TimeGenerated) : (TimeSpan?)null,
                    networdAddress: logon.SourceAddress,
                    accessType: logon.AccessType.ToString()));
            }

            list.AddRange(orphans);

            return list.OrderBy(s => s.LogOnTime).ToList();
        }

        private static List<SessionCandidate> MergeLinkedSessions(IReadOnlyList<LogOnEntry> logOns)
        {
            var result = new List<SessionCandidate>();
            var merged = new HashSet<LogOnEntry>(ReferenceComparer.Instance);

            foreach (var logon in logOns)
            {
                if (merged.Contains(logon)) continue;

                var partner = FindLinkedPartner(logon, logOns, merged);
                var candidate = new SessionCandidate();
                candidate.Ids.Add(logon.Index);

                if (partner != null)
                {
                    merged.Add(partner);
                    candidate.Ids.Add(partner.Index);

                    // Riga principale: la sessione non elevata (quella in cui l'utente lavora e che riceve il 4647)
                    candidate.Primary = logon.IsElevated && !partner.IsElevated ? partner : logon;
                    candidate.LinkedIndex = candidate.Primary == logon ? partner.Index : logon.Index;
                }
                else
                {
                    candidate.Primary = logon;
                }

                merged.Add(logon);
                result.Add(candidate);
            }

            return result;
        }

        // LogOnEntry.Equals confronta il solo Logon ID: due accessi con lo stesso ID in avvii diversi sono distinti,
        // quindi gli insiemi di lavoro vanno confrontati per riferimento
        private sealed class ReferenceComparer : IEqualityComparer<LogOnEntry>
        {
            public static readonly ReferenceComparer Instance = new ReferenceComparer();
            public bool Equals(LogOnEntry x, LogOnEntry y) => ReferenceEquals(x, y);
            public int GetHashCode(LogOnEntry obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
        }

        private static readonly TimeSpan LinkedLogonWindow = TimeSpan.FromSeconds(5);

        private static LogOnEntry FindLinkedPartner(LogOnEntry logon, IReadOnlyList<LogOnEntry> all, HashSet<LogOnEntry> merged)
        {
            if (!IsRealLogonId(logon.LinkedLogonId)) return null;

            return all.FirstOrDefault(p => !ReferenceEquals(p, logon)
                && !merged.Contains(p)
                && string.Equals(p.Index, logon.LinkedLogonId, StringComparison.OrdinalIgnoreCase)
                && string.Equals(p.LinkedLogonId, logon.Index, StringComparison.OrdinalIgnoreCase)
                && (p.TimeGenerated - logon.TimeGenerated).Duration() <= LinkedLogonWindow);
        }

        private static bool IsRealLogonId(string id)
            => !string.IsNullOrWhiteSpace(id)
               && !string.Equals(id, "0x0", StringComparison.OrdinalIgnoreCase)
               && !string.Equals(id, "-", StringComparison.Ordinal);

        private static List<LogOnEntry> GetLogOnEntries(IReadOnlyList<IEventRecord> events)
        {
            var logOnEntries = events.Where(ev =>
                ev.EventId == AppConstants.EventLog.LogonEventId &&
                ev.Source == AppConstants.EventLog.SecurityProviderName &&
                ev.ReplacementStrings != null &&
                ev.ReplacementStrings.Length > 18).ToList();

            var filteredByAccessType = FilterByAccessType(logOnEntries).ToList();

            var filteredByAccountName = filteredByAccessType.Where(ev =>
                !ev.ReplacementStrings[5].StartsWith("UMFD-") &&
                !ev.ReplacementStrings[5].StartsWith("DWM-")).ToList();

            return RemoveDuplicates(BuildLogOnEntries(filteredByAccountName).ToList());
        }

        private static IEnumerable<LogoffRecord> GetLogoffRecords(IReadOnlyList<IEventRecord> events)
        {
            foreach (var entry in events)
            {
                if (entry.Source != AppConstants.EventLog.SecurityProviderName) continue;

                LogoffKind kind;
                if (entry.EventId == AppConstants.EventLog.LogoffEventId) kind = LogoffKind.UserInitiated;
                else if (entry.EventId == AppConstants.EventLog.SessionEndedEventId) kind = LogoffKind.SessionEnded;
                else continue;

                if (entry.ReplacementStrings == null || entry.ReplacementStrings.Length < 4) continue;

                yield return new LogoffRecord
                {
                    Index = entry.ReplacementStrings[3],
                    Time = entry.TimeGenerated,
                    AccountName = entry.ReplacementStrings[1],
                    DomainName = entry.ReplacementStrings[2],
                    MachineName = entry.MachineName,
                    Kind = kind
                };
            }
        }

        // Tipi guidati da persona: 2 interattivo, 9 nuove credenziali, 10 remoto, 11 in cache.
        // Fuori restano sistema e rete (0), rete (3), batch (4), servizio (5), cleartext (8) e lo sblocco (7):
        // lo sblocco dello schermo non e' una sessione (e crea due Logon ID chiusi nello stesso secondo).
        private static readonly int[] HumanAccessTypes = { 2, 9, 10, 11 };

        private static IEnumerable<IEventRecord> FilterByAccessType(IEnumerable<IEventRecord> events)
            => events.Where(ev =>
                int.TryParse(ev.ReplacementStrings[8], out int accessType) &&
                HumanAccessTypes.Contains(accessType));

        private static readonly System.Text.RegularExpressions.Regex LogonIdPattern =
            new System.Text.RegularExpressions.Regex(@"^0x[0-9a-fA-F]+$", System.Text.RegularExpressions.RegexOptions.Compiled);

        private static readonly System.Text.RegularExpressions.Regex ElevatedTokenPattern =
            new System.Text.RegularExpressions.Regex(@"^%%\d+$", System.Text.RegularExpressions.RegexOptions.Compiled);

        /// <summary>
        /// TargetLinkedLogonId ed ElevatedToken sono SEMPRE gli ultimi due campi del 4624 v2, ma la loro posizione
        /// dipende dalla build: 27 campi (25, 26) nelle versioni documentate, 28 campi (26, 27) in Windows 11 recente
        /// (un campo aggiunto prima). Con indici fissi i gemelli non venivano uniti. Si leggono dalla fine, verificando
        /// la forma dei valori; sui sistemi piu' vecchi (meno di 27 campi) non ci sono.
        /// </summary>
        internal static void ExtractLinkedFields(string[] values, out string linkedLogonId, out string elevatedToken)
        {
            linkedLogonId = null;
            elevatedToken = null;

            if (values == null || values.Length < 27) return;

            var linked = values[values.Length - 2];
            var elevated = values[values.Length - 1];

            if (linked != null && LogonIdPattern.IsMatch(linked)) linkedLogonId = linked;
            if (elevated != null && ElevatedTokenPattern.IsMatch(elevated)) elevatedToken = elevated;
        }

        private static IEnumerable<LogOnEntry> BuildLogOnEntries(List<IEventRecord> entries)
        {
            foreach (var entry in entries)
            {
                LogOnEntry parsed;

                try
                {
                    ExtractLinkedFields(entry.ReplacementStrings, out var linkedLogonId, out var elevatedToken);

                    parsed = new LogOnEntry(
                        eventId: entry.EventId,
                        machineName: entry.MachineName,
                        index: entry.ReplacementStrings[7],
                        timeGenerated: entry.TimeGenerated,
                        accountName: entry.ReplacementStrings[5],
                        domainName: entry.ReplacementStrings[6],
                        group: entry.ReplacementStrings[2],
                        accessType: Convert.ToInt32(entry.ReplacementStrings[8]),
                        sourceAddress: entry.ReplacementStrings[18])
                    {
                        LinkedLogonId = linkedLogonId,
                        ElevatedToken = elevatedToken
                    };
                }
                catch
                {
                    continue;
                }

                yield return parsed;
            }
        }

        // Lo stesso 4624 puo' comparire due volte (es. letto due volte): stesso Logon ID e stesso istante.
        // Il solo Logon ID non basta: ripartono a ogni avvio e possono ripetersi, e scartare il secondo farebbe
        // sparire un accesso reale.
        private static List<LogOnEntry> RemoveDuplicates(List<LogOnEntry> entries)
        {
            if (entries == null) throw new ArgumentNullException(nameof(entries));

            return entries
                .GroupBy(e => (e.Index?.ToLowerInvariant(), e.TimeGenerated))
                .Select(g => g.First())
                .ToList();
        }
    }
}
