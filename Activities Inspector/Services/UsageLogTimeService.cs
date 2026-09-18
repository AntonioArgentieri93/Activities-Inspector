using CSharpFunctionalExtensions;
using Activities_Inspector.Constants;
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

        private readonly Evidence.IEvidenceSourceProvider _sources;

        public UsageLogTimeService(Evidence.IEvidenceSourceProvider sources)
        {
            _sources = sources;
        }

        public async Task<Result<List<IEventRecord>>> GetSystemEventsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                if (!_sources.Current.IsLive)
                {
                    var path = _sources.Current.GetEventLogPath(LogFilter);
                    return Result.Success(await Task.Run(() => Evidence.EvtxFileReader.ReadEvents(path), cancellationToken));
                }

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

        public IEnumerable<UsageInfo> BuildUsageInfo(IEnumerable<IEventRecord> events)
        {
            if (events == null) throw new ArgumentNullException(nameof(events));

            var points = events
                .Where(IsStartEvent)
                .Select(ev => (
                    Time: ev.TimeGenerated,
                    Machine: ev.MachineName,
                    IsStart: true,
                    IsCrashBoot: ev.EventId == AppConstants.EventLog.UnexpectedShutdownEventId))
                .Concat(events
                    .Where(IsEndEvent)
                    .Select(ev => (
                        Time: ev.TimeGenerated,
                        Machine: ev.MachineName,
                        IsStart: false,
                        IsCrashBoot: false)))
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

        private static bool IsStartEvent(IEventRecord ev)
        {
            if (ev.CategoryNumber == 5)
                return false;

            // EventId e' l'ID puro (niente severity bit come in InstanceId):
            // verificato contro la nota storica qui sotto, ora superflua.
            bool isBoot = ev.EventId == AppConstants.EventLog.BootEventId
                && ev.Source == AppConstants.EventLog.EventLogProviderName
                && ev.EntryType == EventLogEntryType.Information;

            // Il 41 (unexpected shutdown) è di livello Critical, non Information.
            bool isCrash = ev.EventId == AppConstants.EventLog.UnexpectedShutdownEventId
                && ev.Source == AppConstants.EventLog.KernelPowerProviderName;

            return isBoot || isCrash;
        }

        private static bool IsEndEvent(IEventRecord ev)
        {
            if (ev.CategoryNumber == 5 || ev.EntryType != EventLogEntryType.Information)
                return false;

            return (ev.EventId == AppConstants.EventLog.ShutdownEventId &&
                    ev.Source == AppConstants.EventLog.EventLogProviderName)
                || (ev.EventId == AppConstants.EventLog.SleepEventId &&
                    ev.Source == AppConstants.EventLog.KernelPowerProviderName);
        }

        internal static List<(IntervalEntry Interval, string MachineName)> PairIntervals(
            List<(DateTime Time, string Machine, bool IsStart, bool IsCrashBoot)> points)
        {
            var starts = points.Where(p => p.IsStart).OrderBy(p => p.Time).ToList();
            var ends = points.Where(p => !p.IsStart).OrderBy(p => p.Time).ToList();

            var result = new List<(IntervalEntry Interval, string MachineName)>();
            var consumed = new HashSet<int>();

            foreach (var end in ends)
            {
                var candidateIndex = -1;

                for (var i = starts.Count - 1; i >= 0; i--)
                {
                    if (!consumed.Contains(i) && starts[i].Time < end.Time)
                    {
                        candidateIndex = i;
                        break;
                    }
                }

                if (candidateIndex >= 0)
                {
                    consumed.Add(candidateIndex);
                    var start = starts[candidateIndex];
                    var interval = new IntervalEntry(start.Time, end.Time)
                    {
                        StartedAfterCrash = start.IsCrashBoot
                    };
                    result.Add((interval, start.Machine));
                }
            }

            if (starts.Count == 0)
            {
                result.Add((new IntervalEntry(null), string.Empty));
            }
            else
            {
                var last = starts[starts.Count - 1];
                var openInterval = new IntervalEntry(last.Time, null)
                {
                    StartedAfterCrash = last.IsCrashBoot
                };
                result.Add((openInterval, last.Machine));
            }

            return result;
        }

    }
}
