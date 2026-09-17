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
                .Select(ev => (
                    Time: ev.TimeGenerated,
                    Machine: ev.MachineName,
                    IsStart: true,
                    IsCrashBoot: ev.InstanceId == AppConstants.EventLog.UnexpectedShutdownEventId))
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

        private static bool IsStartEvent(EventLogEntry ev)
        {
            if (ev.CategoryNumber == 5)
                return false;

            // NOTA: si usa EventID (obsoleto) e non InstanceId: per gli eventi
            // scritti dai servizi (es. 6005/6006) InstanceId contiene anche i
            // bit di severity (es. 2147489653), quindi il match esatto richiede
            // EventID. Verificato empiricamente sul log di sistema.
#pragma warning disable CS0618
            bool isBoot = ev.EventID == AppConstants.EventLog.BootEventId
                && ev.Source == AppConstants.EventLog.EventLogProviderName
                && ev.EntryType == EventLogEntryType.Information;

            // Il 41 (unexpected shutdown) è di livello Critical, non Information.
            bool isCrash = ev.EventID == AppConstants.EventLog.UnexpectedShutdownEventId
                && ev.Source == AppConstants.EventLog.KernelPowerProviderName;
#pragma warning restore CS0618

            return isBoot || isCrash;
        }

        private static bool IsEndEvent(EventLogEntry ev)
        {
            if (ev.CategoryNumber == 5 || ev.EntryType != EventLogEntryType.Information)
                return false;

#pragma warning disable CS0618
            return (ev.EventID == AppConstants.EventLog.ShutdownEventId &&
                    ev.Source == AppConstants.EventLog.EventLogProviderName)
                || (ev.EventID == AppConstants.EventLog.SleepEventId &&
                    ev.Source == AppConstants.EventLog.KernelPowerProviderName);
#pragma warning restore CS0618
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
