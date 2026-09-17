using CSharpFunctionalExtensions;
using Activities_Inspector.Models;
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
            catch (Exception ex) when (!(ex is OperationCanceledException))
            {
                return Result.Failure<List<EventLogEntry>>(ex.ToString());
            }
        }

        public IEnumerable<UsageInfo> BuildUsageInfo(IEnumerable<EventLogEntry> events)
        {
            if (events == null) throw new ArgumentNullException(nameof(events));

            var points = events
                .Where(IsStartEvent)
                .Select(ev => (Time: ev.TimeGenerated, Machine: ev.MachineName, IsStart: true))
                .Concat(events
                    .Where(IsEndEvent)
                    .Select(ev => (Time: ev.TimeGenerated, Machine: ev.MachineName, IsStart: false)))
                .ToList();

            var pairs = PairIntervals(points)
                .Where(p => p.Interval.Start != DateTime.MinValue)
                .ToArray();

            for (var i = 0; i < pairs.Length; i++)
            {
                var duration = pairs[i].Interval.End != null
                    ? pairs[i].Interval.End.Value.Subtract(pairs[i].Interval.Start)
                    : DateTime.Now.Subtract(pairs[i].Interval.Start);

                yield return new UsageInfo(pairs[i].Interval, duration, pairs[i].MachineName);
            }
        }

        private static bool IsStartEvent(EventLogEntry ev)
        {
            return (ev.InstanceId == 1 || ev.InstanceId == 41)
                && ev.CategoryNumber != 5
                && ev.EntryType == EventLogEntryType.Information;
        }

        private static bool IsEndEvent(EventLogEntry ev)
        {
            return (ev.InstanceId == 6006 || ev.InstanceId == 42)
                && ev.CategoryNumber != 5
                && ev.EntryType == EventLogEntryType.Information;
        }

        internal static List<(IntervalEntry Interval, string MachineName)> PairIntervals(
            List<(DateTime Time, string Machine, bool IsStart)> points)
        {
            var starts = points.Where(p => p.IsStart).OrderBy(p => p.Time).ToList();
            var ends = points.Where(p => !p.IsStart).OrderBy(p => p.Time).ToList();

            var result = new List<(IntervalEntry Interval, string MachineName)>();

            foreach (var end in ends)
            {
                var candidates = starts.Where(s => s.Time < end.Time).ToList();

                if (candidates.Count > 0)
                {
                    var start = candidates[candidates.Count - 1];
                    result.Add((new IntervalEntry(start.Time, end.Time), start.Machine));
                }
            }

            if (starts.Count == 0)
            {
                result.Add((new IntervalEntry(null), string.Empty));
            }
            else
            {
                var last = starts[starts.Count - 1];
                result.Add((new IntervalEntry(last.Time, null), last.Machine));
            }

            return result;
        }

    }
}
