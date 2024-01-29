using CSharpFunctionalExtensions;
using ProgettoInformaticaForense_Argentieri.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace ProgettoInformaticaForense_Argentieri.Services
{
    public class UsageLogTimeService : IUsageLogTimeService
    {
        private const string LOG_FILTER = "System";

        public async Task<Result<List<EventLogEntry>>> GetSystemEventsAsync()
        {
            var taskCompletionSource = new TaskCompletionSource<Result<List<EventLogEntry>>>(
                TaskCreationOptions.RunContinuationsAsynchronously);

            var myLog = new EventLog();
            myLog.Log = LOG_FILTER;

            var tmp = new List<EventLogEntry>();

            try
            {
                await Task.Run(() =>
                {
                    foreach (var @event in myLog.Entries)
                    {
                        var logEntry = (EventLogEntry)@event;
                        tmp.Add(logEntry);
                    }
                });

                taskCompletionSource.SetResult(Result.Success(tmp));
            }
            catch (Exception ex)
            {
                taskCompletionSource.SetResult(Result.Failure<List<EventLogEntry>>(ex.Message));
            }

            return taskCompletionSource.Task.Result;
        }

        public IEnumerable<UsageInfo> BuildUsageInfo(IEnumerable<EventLogEntry> events)
        {
            if (events == null) throw new ArgumentNullException(nameof(events));

            var machineNames = events.Where(ev => ev.EventID == 1 && ev.CategoryNumber != 5 || ev.EventID == 41 && 
                ev.CategoryNumber != 5).Select(ev => ev.MachineName).ToArray();

            var intervals = GetIntervals(events).Where(interval => interval.Start != DateTime.MinValue).ToArray();

            TimeSpan duration;

            for (var i = 0; i < intervals.Length; i++)
            {
                if (intervals[i].End != null)
                {
                    duration = intervals[i].End.Value.Subtract(intervals[i].Start);
                }
                else
                {
                    duration = DateTime.Now.Subtract(intervals[i].Start);
                }

                if (machineNames.Count() != 0)
                {
                    yield return new UsageInfo(intervals[i], duration, machineNames[i]);
                }
                else
                {
                    yield return new UsageInfo(intervals[i], duration, string.Empty);
                }
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
                    var interval = new IntervalEntry(startItem.TimeGenerated, endItem.TimeGenerated);
                    yield return interval;
                }
                else continue;
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
