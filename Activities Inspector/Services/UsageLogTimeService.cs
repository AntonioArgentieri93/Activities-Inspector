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
    public class UsageLogTimeService : IUsageLogTimeService
    {
        private const string LogFilter = "System";

        private readonly Evidence.IEvidenceSourceProvider _sources;

        public UsageLogTimeService(Evidence.IEvidenceSourceProvider sources)
        {
            _sources = sources;
        }

        public IReadOnlyList<IntegrityRecord> LastIntegrityManifest { get; private set; }
            = new List<IntegrityRecord>();

        public async Task<Result<List<IEventRecord>>> GetSystemEventsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var manifest = new List<IntegrityRecord>();
                LastIntegrityManifest = manifest;

                if (!_sources.Current.IsLive)
                {
                    var path = _sources.Current.GetEventLogPath(LogFilter);
                    manifest.Add(IntegrityHasher.HashFile(path, EntryType.TimeIntervals));
                    return Result.Success(await Task.Run(() => Evidence.EvtxFileReader.ReadEvents(path), cancellationToken));
                }

                manifest.Add(IntegrityRecord.LiveSource(EntryType.TimeIntervals,
                    "Registro System (API live)"));

                using var myLog = new EventLog { Log = LogFilter };
                var entries = new List<IEventRecord>();

                await Task.Run(() =>
                {
                    foreach (EventLogEntry entry in myLog.Entries)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        entries.Add(new LiveEventRecord(entry));
                    }
                }, cancellationToken);

                return Result.Success(entries);
            }
            catch (Exception ex) when (!(ex is OperationCanceledException))
            {
                return Result.Failure<List<IEventRecord>>(ex.ToString());
            }
        }


        /// <param name="observedUntil">
        /// Fino a quando considerare acceso un intervallo senza fine: in live l'ora corrente; per un'immagine non
        /// si conosce il momento dell'acquisizione, quindi (null) si usa l'ultimo evento del registro. Senza questo
        /// la durata dell'intervallo aperto di un'immagine arrivava fino a "adesso" del PC dell'analista.
        /// </param>
        public IEnumerable<UsageInfo> BuildUsageInfo(IEnumerable<IEventRecord> events, DateTime? observedUntil = null)
        {
            if (events == null) throw new ArgumentNullException(nameof(events));

            var all = events as IList<IEventRecord> ?? events.ToList();

            var crashMarkers = all.Where(IsCrashMarker).Select(e => e.TimeGenerated).OrderBy(t => t).ToList();

            var points = new List<IntervalPoint>();
            foreach (var ev in all)
            {
                if (IsBootEvent(ev))
                    points.Add(new IntervalPoint(ev.TimeGenerated, ev.MachineName, IntervalPointKind.Boot,
                        HasMarkerNear(crashMarkers, ev.TimeGenerated)));
                else if (IsWakeEvent(ev))
                    points.Add(new IntervalPoint(ev.TimeGenerated, ev.MachineName, IntervalPointKind.Wake));
                else if (IsShutdownEvent(ev))
                    points.Add(new IntervalPoint(ev.TimeGenerated, ev.MachineName, IntervalPointKind.Shutdown));
                else if (IsSleepEvent(ev))
                    points.Add(new IntervalPoint(ev.TimeGenerated, ev.MachineName, IntervalPointKind.Sleep));
            }

            var eventTimes = all.Select(e => e.TimeGenerated).OrderBy(t => t).ToList();
            var osStartTimes = all.Where(IsOsStartEvent).Select(e => e.TimeGenerated).OrderBy(t => t).ToList();

            var lastObserved = observedUntil
                ?? (eventTimes.Count > 0 ? eventTimes[eventTimes.Count - 1] : DateTime.Now);

            var pairs = PairIntervals(points, eventTimes, osStartTimes)
                .Where(p => p.Interval.Start != DateTime.MinValue)
                .ToList();

            var result = new List<UsageInfo>(pairs.Count);
            foreach (var pair in pairs)
            {
                var end = pair.Interval.End ?? lastObserved;
                var duration = end > pair.Interval.Start ? end - pair.Interval.Start : TimeSpan.Zero;
                result.Add(new UsageInfo(pair.Interval, duration, pair.MachineName));
            }

            return result;
        }

        // Avvio del servizio Registro eventi (6005): inizio di un periodo di accensione.
        private static bool IsBootEvent(IEventRecord ev)
            => ev.CategoryNumber != 5
               && ev.EventId == AppConstants.EventLog.BootEventId
               && ev.Source == AppConstants.EventLog.EventLogProviderName
               && ev.EntryType == EventLogEntryType.Information;

        // Risveglio da sospensione/ibernazione (Power-Troubleshooter 1): inizio di un nuovo periodo di accensione.
        // Senza questo, il tempo d'uso dopo ogni sospensione spariva dai risultati.
        private static bool IsWakeEvent(IEventRecord ev)
            => ev.EventId == AppConstants.EventLog.WakeEventId
               && ev.Source == AppConstants.EventLog.PowerTroubleshooterProviderName
               && ev.EntryType == EventLogEntryType.Information;

        // Arresto regolare (6006).
        private static bool IsShutdownEvent(IEventRecord ev)
            => ev.CategoryNumber != 5
               && ev.EntryType == EventLogEntryType.Information
               && ev.EventId == AppConstants.EventLog.ShutdownEventId
               && ev.Source == AppConstants.EventLog.EventLogProviderName;

        // Ingresso in sospensione/ibernazione (Kernel-Power 42).
        private static bool IsSleepEvent(IEventRecord ev)
            => ev.CategoryNumber != 5
               && ev.EntryType == EventLogEntryType.Information
               && ev.EventId == AppConstants.EventLog.SleepEventId
               && ev.Source == AppConstants.EventLog.KernelPowerProviderName;

        // Indizi di un arresto non regolare, registrati al riavvio: Kernel-Power 41 e EventLog 6008. Il 41 compare
        // qualche secondo PRIMA del 6005 e NON è un inizio di intervallo (prima lo era, e generava avvii "fantasma").
        private static bool IsCrashMarker(IEventRecord ev)
            => (ev.EventId == AppConstants.EventLog.UnexpectedShutdownEventId
                && ev.Source == AppConstants.EventLog.KernelPowerProviderName)
               || (ev.EventId == AppConstants.EventLog.PreviousShutdownUnexpectedEventId
                   && ev.Source == AppConstants.EventLog.EventLogProviderName);

        // Avvio del sistema operativo (Kernel-General 12): delimita l'inizio di una nuova sessione di avvio.
        private static bool IsOsStartEvent(IEventRecord ev)
            => ev.EventId == AppConstants.EventLog.OsStartEventId
               && ev.Source == AppConstants.EventLog.KernelGeneralProviderName;

        private static readonly TimeSpan CrashMarkerBefore = TimeSpan.FromMinutes(3);
        private static readonly TimeSpan CrashMarkerAfter = TimeSpan.FromSeconds(60);

        private static bool HasMarkerNear(IReadOnlyList<DateTime> markers, DateTime boot)
            => markers.Any(m => m >= boot - CrashMarkerBefore && m <= boot + CrashMarkerAfter);

        /// <summary>
        /// Ricostruzione cronologica degli intervalli di accensione:
        /// avvio (6005) o risveglio apre un intervallo; arresto (6006) o sospensione lo chiude.
        /// Se arriva un nuovo avvio mentre un intervallo è ancora aperto, il precedente si è interrotto senza
        /// registrare l'arresto (crash, blackout, spegnimento forzato): resta nell'elenco con fine STIMATA
        /// (ultimo evento registrato prima dell'avvio successivo del sistema operativo) e tipo "Anomalo".
        /// Un intervallo ancora aperto alla fine del log è "In corso".
        /// </summary>
        internal static List<(IntervalEntry Interval, string MachineName)> PairIntervals(
            IReadOnlyList<IntervalPoint> points,
            IReadOnlyList<DateTime> eventTimes = null,
            IReadOnlyList<DateTime> osStartTimes = null)
        {
            var result = new List<(IntervalEntry Interval, string MachineName)>();

            var ordered = points
                .OrderBy(p => p.Time)
                .ThenBy(p => p.Kind == IntervalPointKind.Shutdown || p.Kind == IntervalPointKind.Sleep ? 0 : 1)
                .ToList();

            IntervalEntry open = null;
            var openMachine = string.Empty;

            foreach (var point in ordered)
            {
                switch (point.Kind)
                {
                    case IntervalPointKind.Boot:
                        if (open != null)
                        {
                            CloseAsUnexpected(open, point.Time, eventTimes, osStartTimes);
                            result.Add((open, openMachine));
                        }

                        open = new IntervalEntry(point.Time, null) { StartedAfterCrash = point.CrashBoot };
                        openMachine = point.Machine;
                        break;

                    case IntervalPointKind.Wake:
                        // Il risveglio apre un intervallo solo se il precedente è chiuso (di norma dalla sospensione)
                        if (open == null)
                        {
                            open = new IntervalEntry(point.Time, null);
                            openMachine = point.Machine;
                        }
                        break;

                    case IntervalPointKind.Shutdown:
                    case IntervalPointKind.Sleep:
                        if (open != null)
                        {
                            open.End = point.Time < open.Start ? open.Start : point.Time;
                            open.EndKind = point.Kind == IntervalPointKind.Sleep ? IntervalEndKind.Sleep : IntervalEndKind.Shutdown;
                            result.Add((open, openMachine));
                            open = null;
                        }
                        // Una fine senza inizio (log che comincia a metà) non produce alcun intervallo
                        break;
                }
            }

            if (open != null)
                result.Add((open, openMachine));

            if (!ordered.Any(p => p.Kind == IntervalPointKind.Boot || p.Kind == IntervalPointKind.Wake))
                result.Add((new IntervalEntry(null), string.Empty));

            return result;
        }

        private static void CloseAsUnexpected(IntervalEntry interval, DateTime nextBoot,
            IReadOnlyList<DateTime> eventTimes, IReadOnlyList<DateTime> osStartTimes)
        {
            // Limite: primo avvio del sistema operativo dopo l'inizio dell'intervallo (precede il 6005 del riavvio);
            // se non c'è, il nuovo avvio stesso
            var limit = nextBoot;
            if (osStartTimes != null)
            {
                foreach (var os in osStartTimes)
                {
                    if (os > interval.Start && os <= nextBoot)
                    {
                        limit = os;
                        break;
                    }
                }
            }

            var lastEvent = LastBefore(eventTimes, limit);
            interval.End = lastEvent.HasValue && lastEvent.Value > interval.Start ? lastEvent.Value : interval.Start;
            interval.EndKind = IntervalEndKind.Unexpected;
        }

        /// <summary>Ultimo istante strettamente precedente a <paramref name="limit"/> in una lista ordinata.</summary>
        internal static DateTime? LastBefore(IReadOnlyList<DateTime> sortedTimes, DateTime limit)
        {
            if (sortedTimes == null || sortedTimes.Count == 0) return null;

            int lo = 0, hi = sortedTimes.Count - 1, found = -1;
            while (lo <= hi)
            {
                var mid = lo + (hi - lo) / 2;
                if (sortedTimes[mid] < limit) { found = mid; lo = mid + 1; }
                else hi = mid - 1;
            }

            return found >= 0 ? sortedTimes[found] : (DateTime?)null;
        }
    }

    internal enum IntervalPointKind
    {
        Boot,
        Wake,
        Shutdown,
        Sleep
    }

    internal readonly struct IntervalPoint
    {
        public IntervalPoint(DateTime time, string machine, IntervalPointKind kind, bool crashBoot = false)
        {
            Time = time;
            Machine = machine;
            Kind = kind;
            CrashBoot = crashBoot;
        }

        public DateTime Time { get; }
        public string Machine { get; }
        public IntervalPointKind Kind { get; }
        /// <summary>Solo per Boot: l'avvio segue un arresto non regolare (Kernel-Power 41 / EventLog 6008).</summary>
        public bool CrashBoot { get; }
    }
}
