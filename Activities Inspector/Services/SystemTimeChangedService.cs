using CSharpFunctionalExtensions;
using Activities_Inspector.Constants;
using Activities_Inspector.Models;
using Activities_Inspector.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Activities_Inspector.Services
{
    public class SystemTimeChangedService : ISystemTimeChangedService
    {
        private readonly Evidence.IEvidenceSourceProvider _sources;

        public SystemTimeChangedService(Evidence.IEvidenceSourceProvider sources)
        {
            _sources = sources;
        }

        public IReadOnlyList<IntegrityRecord> LastIntegrityManifest { get; private set; }
            = new List<IntegrityRecord>();

        public async Task<Result<List<SystemTimeChangedEntry>>> GetSystemTimeChangedEntriesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var manifest = new List<IntegrityRecord>();
                LastIntegrityManifest = manifest;

                if (_sources.Current.IsLive)
                {
                    manifest.Add(IntegrityRecord.LiveSource(EntryType.SystemTimeChanged,
                        "Registro Sicurezza (API live)"));
                }

                var logEntries = await GetSystemTimeChangedEventLogEntriesAsync(manifest, cancellationToken);

                // Parsing e filtri sull'intero log: CPU-bound su thread pool,
                // mai sullo UI thread.
                var entries = await Task.Run(() =>
                {
                    var list = new List<SystemTimeChangedEntry>();

                    foreach (var entry in logEntries)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        if (entry.ReplacementStrings.Length < 8) continue;

                        // SID di LOCAL SERVICE: indipendente dalla lingua del
                        // sistema (il vecchio controllo sul nome perdeva le
                        // altre localizzazioni, es. francese/tedesco).
                        if (entry.ReplacementStrings[0] == @"S-1-5-19") continue;

                        if (entry.ReplacementStrings[7] == @"C:\Windows\System32\svchost.exe") continue;

                        if (!DateTime.TryParseExact(entry.TimeGenerated.ToString("dd/M/yyyy HH:mm:ss"), "dd/M/yyyy HH:mm:ss",
                            DateTimeFormatInfo.InvariantInfo, DateTimeStyles.None, out DateTime timeGenerated))
                        {
                            continue;
                        }

                        if (!DateTime.TryParse(entry.ReplacementStrings[4], null, DateTimeStyles.RoundtripKind, out DateTime oldTime) ||
                            !DateTime.TryParse(entry.ReplacementStrings[5], null, DateTimeStyles.RoundtripKind, out DateTime newTime))
                        {
                            continue;
                        }

                        oldTime = DateBuilder.ToLocal(oldTime);
                        newTime = DateBuilder.ToLocal(newTime);

                        if (oldTime == newTime) continue;

                        list.Add(new SystemTimeChangedEntry(entry.ReplacementStrings[1],
                            DateBuilder.BuildFromDateTime(timeGenerated),
                            DateBuilder.BuildFromString(oldTime.ToString()),
                            DateBuilder.BuildFromString(newTime.ToString())));
                    }

                    return list.OrderBy(ee => ee.TimeGenerated).ToList();
                }, cancellationToken);

                return Result.Success(entries);
            }
            catch (Exception ex) when (!(ex is OperationCanceledException))
            {
                return Result.Failure<List<SystemTimeChangedEntry>>(ex.ToString());
            }
        }

        private async Task<List<IEventRecord>> GetSystemTimeChangedEventLogEntriesAsync(List<IntegrityRecord> manifest, CancellationToken cancellationToken = default)
        {
            if (!_sources.Current.IsLive)
            {
                var path = _sources.Current.GetEventLogPath(AppConstants.EventLog.SecurityLog);
                manifest.Add(IntegrityHasher.HashFile(path, EntryType.SystemTimeChanged));
                return await Task.Run(() => Evidence.EvtxFileReader.ReadEvents(path)
                    .Where(ev => ev.EventId == AppConstants.EventLog.SystemTimeChangedEventId &&
                        ev.Source == AppConstants.EventLog.SecurityProviderName)
                    .ToList(), cancellationToken);
            }

            return await Task.Run(() =>
            {
                using var eventLog = new EventLog { Log = AppConstants.EventLog.SecurityLog };
                var entries = new List<IEventRecord>();

                foreach (EventLogEntry entry in eventLog.Entries)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var record = new LiveEventRecord(entry);
                    if (record.EventId == AppConstants.EventLog.SystemTimeChangedEventId &&
                        record.Source == AppConstants.EventLog.SecurityProviderName)
                        entries.Add(record);
                }

                return entries;
            }, cancellationToken);
        }
    }
}