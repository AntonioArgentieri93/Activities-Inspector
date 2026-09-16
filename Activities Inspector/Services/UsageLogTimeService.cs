using CSharpFunctionalExtensions;
using ProgettoInformaticaForense_Argentieri.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ProgettoInformaticaForense_Argentieri.Services
{
    public class UsageLogTimeService : IUsageLogTimeService
    {
        private const string LogFilter = "System";

        public async Task<Result<List<EventLogEntry>>> GetSystemEventsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                using var myLog = new EventLog { Log = LogFilter };
                var entries = new List<EventLogEntry>();

                await Task.Run(() =>
                {
                    foreach (EventLogEntry entry in myLog.Entries)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        entries.Add(entry);
                    }
                }, cancellationToken);

                return Result.Success(entries);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                return Result.Failure<List<EventLogEntry>>(ex.ToString());
            }
        }

        public IEnumerable<UsageInfo> BuildUsageInfo(IEnumerable<EventLogEntry> events)
        {
            if (events == null) throw new ArgumentNullException(nameof(events));

            var machineNames = events.Where(ev => ev.EventID == 1 && ev.CategoryNumber != 5 || ev.EventID == 41 && 
                ev.CategoryNumber != 5).Select(ev => ev.MachineName).ToArray();

            var intervals = GetIntervals(events).Where(interval => interval.Start != DateTime.MinValue).ToArray();

            for (var i = 0; i < intervals.Length; i++)
            {
                var duration = intervals[i].End != null
                    ? intervals[i].End.Value.Subtract(intervals[i].Start)
                    : DateTime.Now.Subtract(intervals[i].Start);

                var machineName = machineNames.Length > i ? machineNames[i] : string.Empty;
                yield return new UsageInfo(intervals[i], duration, machineName);
            }
        }

        private IEnumerable<IntervalEntry> GetIntervals(IEnumerable<EventLogEntry> events)
        {
            if (events == null) throw new ArgumentNullException(nameof(events));

            var start = events.Where(ev => ev.EventID == 1 && ev.CategoryNumber != 5 || 
                ev.EventID == 41 && ev.CategoryNumber != 5 &&
                ev.EntryType == EventLogEntryType.Information).ToList();
            var end = events.Where(ev => ev.EventID == 6006 && ev.CategoryNumber != 5 || 
                ev.EventID == 42 && ev.CategoryNumber != 5 &&
                ev.EntryType == EventLogEntryType.Information).ToList();

            for (var i = 0; i < end.Count; i++)
            {
                var endItem = end[i];
                var startItem = start.Where(it => it.TimeGenerated < endItem.TimeGenerated).LastOrDefault();

                if (startItem != null)
                {
                    yield return new IntervalEntry(startItem.TimeGenerated, endItem.TimeGenerated);
                }
            }

            var lastStartItem = start.LastOrDefault();
            if (lastStartItem == null)
            {
                yield return new IntervalEntry(null);
            }
            else
            {
                yield return new IntervalEntry(start.Last().TimeGenerated, null);
            }
        }
    }
}