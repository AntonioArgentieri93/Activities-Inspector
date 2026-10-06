using Activities_Inspector.Models;
using Activities_Inspector.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Xunit;

namespace ActivitiesInspector.UnitTests.Services
{
    public class TimeIntervalsPairingTests
    {
        private static readonly DateTime T0 = new DateTime(2024, 1, 1, 8, 0, 0);
        private static readonly DateTime T1 = new DateTime(2024, 1, 1, 12, 0, 0);
        private static readonly DateTime T2 = new DateTime(2024, 1, 1, 18, 0, 0);

        private static IntervalPoint Boot(DateTime t, string m = "PC", bool crash = false) => new IntervalPoint(t, m, IntervalPointKind.Boot, crash);
        private static IntervalPoint Wake(DateTime t, string m = "PC") => new IntervalPoint(t, m, IntervalPointKind.Wake);
        private static IntervalPoint Shutdown(DateTime t, string m = "PC") => new IntervalPoint(t, m, IntervalPointKind.Shutdown);
        private static IntervalPoint Sleep(DateTime t, string m = "PC") => new IntervalPoint(t, m, IntervalPointKind.Sleep);

        private static List<(IntervalEntry Interval, string MachineName)> Real(List<(IntervalEntry Interval, string MachineName)> pairs)
            => pairs.Where(p => p.Interval.Start != DateTime.MinValue).ToList();

        // --- Semplice avvio/arresto ---

        [Fact]
        public void Boot_Then_Shutdown_Is_One_Closed_Interval()
        {
            var pairs = Real(UsageLogTimeService.PairIntervals(new[] { Boot(T0), Shutdown(T2) }));

            var p = Assert.Single(pairs);
            Assert.Equal(T0, p.Interval.Start);
            Assert.Equal(T2, p.Interval.End);
            Assert.Equal(IntervalEndKind.Shutdown, p.Interval.EndKind);
        }

        [Fact]
        public void Pair_Gets_Machine_Of_Matching_Start()
        {
            var pairs = Real(UsageLogTimeService.PairIntervals(new[] { Boot(T0, "M1"), Shutdown(T1, "M1"), Boot(T1.AddMinutes(5), "M2"), Shutdown(T2, "M2") }));

            Assert.Equal(2, pairs.Count);
            Assert.Equal("M1", pairs[0].MachineName);
            Assert.Equal("M2", pairs[1].MachineName);
        }

        [Fact]
        public void Open_Interval_Is_In_Progress_With_Last_Start_Machine()
        {
            var pairs = Real(UsageLogTimeService.PairIntervals(new[] { Boot(T0, "M1") }));

            var open = Assert.Single(pairs);
            Assert.Equal(T0, open.Interval.Start);
            Assert.Null(open.Interval.End);
            Assert.Equal(IntervalEndKind.InProgress, open.Interval.EndKind);
            Assert.Equal("M1", open.MachineName);
        }

        [Fact]
        public void Unsorted_Input_Is_Ordered_By_Time()
        {
            var pairs = Real(UsageLogTimeService.PairIntervals(new[] { Shutdown(T2), Boot(T0) }));

            var p = Assert.Single(pairs);
            Assert.Equal(T0, p.Interval.Start);
            Assert.Equal(T2, p.Interval.End);
        }

        [Fact]
        public void Empty_Input_Yields_Filterable_Sentinel()
        {
            var single = Assert.Single(UsageLogTimeService.PairIntervals(new List<IntervalPoint>()));

            Assert.Equal(DateTime.MinValue, single.Interval.Start);
        }

        [Fact]
        public void Shutdown_Without_Start_Produces_No_Interval()
        {
            Assert.Empty(Real(UsageLogTimeService.PairIntervals(new[] { Shutdown(T2) })));
        }

        [Fact]
        public void Crash_Boot_Flag_Is_Carried_To_The_Interval()
        {
            var pairs = Real(UsageLogTimeService.PairIntervals(new[] { Boot(T1, crash: true), Shutdown(T2) }));

            Assert.True(pairs[0].Interval.StartedAfterCrash);
        }

        // --- Sospensione e risveglio (prima il tempo dopo il risveglio spariva) ---

        [Fact]
        public void Sleep_Closes_And_Wake_Opens_A_New_Interval()
        {
            var wake = T0.AddHours(3);
            var pairs = Real(UsageLogTimeService.PairIntervals(new[]
            {
                Boot(T0), Sleep(T0.AddHours(2)), Wake(wake), Shutdown(T2)
            }));

            Assert.Equal(2, pairs.Count);
            Assert.Equal(IntervalEndKind.Sleep, pairs[0].Interval.EndKind);
            Assert.Equal(T0.AddHours(2), pairs[0].Interval.End);
            Assert.Equal(wake, pairs[1].Interval.Start);
            Assert.Equal(T2, pairs[1].Interval.End);
            Assert.Equal(IntervalEndKind.Shutdown, pairs[1].Interval.EndKind);
        }

        [Fact]
        public void Wake_While_Already_On_Does_Not_Split_The_Interval()
        {
            var pairs = Real(UsageLogTimeService.PairIntervals(new[] { Boot(T0), Wake(T1), Shutdown(T2) }));

            var p = Assert.Single(pairs);
            Assert.Equal(T0, p.Interval.Start);
            Assert.Equal(T2, p.Interval.End);
        }

        [Fact]
        public void Wake_Without_Previous_Boot_Opens_An_Interval()
        {
            var pairs = Real(UsageLogTimeService.PairIntervals(new[] { Wake(T0), Sleep(T1) }));

            var p = Assert.Single(pairs);
            Assert.Equal(T0, p.Interval.Start);
            Assert.Equal(IntervalEndKind.Sleep, p.Interval.EndKind);
        }

        // --- Interruzione senza evento di arresto (crash/blackout) ---

        [Fact]
        public void New_Boot_While_Open_Closes_Previous_As_Unexpected_With_Last_Event_Estimate()
        {
            var lastEvent = T0.AddHours(1);
            var times = new[] { T0, T0.AddMinutes(30), lastEvent, T2.AddSeconds(-20), T2 };   // dopo lastEvent: sistema spento
            var osStarts = new[] { T0.AddSeconds(-15), T2.AddSeconds(-20) };                   // avvio OS del riavvio, prima del 6005

            var pairs = Real(UsageLogTimeService.PairIntervals(new[] { Boot(T0), Boot(T2, crash: true), Shutdown(T2.AddHours(1)) }, times, osStarts));

            Assert.Equal(2, pairs.Count);
            Assert.Equal(T0, pairs[0].Interval.Start);
            Assert.Equal(IntervalEndKind.Unexpected, pairs[0].Interval.EndKind);
            Assert.Equal(lastEvent, pairs[0].Interval.End);   // ultimo evento prima dell avvio OS successivo
            Assert.False(pairs[0].Interval.StartedAfterCrash);
            Assert.True(pairs[1].Interval.StartedAfterCrash);
            Assert.Equal(IntervalEndKind.Shutdown, pairs[1].Interval.EndKind);
        }

        [Fact]
        public void Interrupted_Interval_Is_Never_Paired_With_A_Later_Unrelated_Shutdown()
        {
            // Regressione: lo stesso avvio veniva accoppiato a uno spegnimento di giorni dopo (intervalli di 19 giorni)
            var crashBoot = T0.AddDays(1);
            var pairs = Real(UsageLogTimeService.PairIntervals(
                new[] { Boot(T0), Boot(crashBoot, crash: true), Shutdown(crashBoot.AddHours(2)) },
                new[] { T0, T0.AddMinutes(10), crashBoot },
                new[] { crashBoot.AddSeconds(-15) }));

            Assert.Equal(2, pairs.Count);
            Assert.True(pairs[0].Interval.End <= T0.AddMinutes(10));
            Assert.True(pairs[0].Interval.End - pairs[0].Interval.Start < TimeSpan.FromHours(1));
        }

        [Fact]
        public void Estimate_Without_Event_Information_Falls_Back_To_Interval_Start()
        {
            var pairs = Real(UsageLogTimeService.PairIntervals(new[] { Boot(T0), Boot(T1) }));

            Assert.Equal(IntervalEndKind.Unexpected, pairs[0].Interval.EndKind);
            Assert.Equal(T0, pairs[0].Interval.End);
        }

        [Theory]
        [InlineData(0, null)]
        [InlineData(5, 4)]
        [InlineData(3, 2)]
        public void LastBefore_Returns_Strictly_Earlier_Time(int limitHours, int? expectedHours)
        {
            var times = Enumerable.Range(0, 5).Select(h => T0.AddHours(h)).ToList();   // 0..4 ore
            var result = UsageLogTimeService.LastBefore(times, T0.AddHours(limitHours));

            Assert.Equal(expectedHours.HasValue ? T0.AddHours(expectedHours.Value) : (DateTime?)null, result);
        }

        // --- Dagli eventi grezzi (come arrivano dal registro System) ---

        private sealed class Ev : IEventRecord
        {
            public Ev(DateTime t, int id, string source, EventLogEntryType type = EventLogEntryType.Information, string machine = "MSI")
            {
                TimeGenerated = t; EventId = id; Source = source; EntryType = type; MachineName = machine;
            }
            public int EventId { get; }
            public string Source { get; }
            public DateTime TimeGenerated { get; }
            public string MachineName { get; }
            public string[] ReplacementStrings => new string[0];
            public short CategoryNumber => 0;
            public EventLogEntryType EntryType { get; }
        }

        private static Ev Boot6005(DateTime t) => new Ev(t, 6005, "EventLog");
        private static Ev Shutdown6006(DateTime t) => new Ev(t, 6006, "EventLog");
        private static Ev Sleep42(DateTime t) => new Ev(t, 42, "Microsoft-Windows-Kernel-Power");
        private static Ev Wake1(DateTime t) => new Ev(t, 1, "Microsoft-Windows-Power-Troubleshooter");
        private static Ev Crash41(DateTime t) => new Ev(t, 41, "Microsoft-Windows-Kernel-Power", EventLogEntryType.Error);
        private static Ev Unexpected6008(DateTime t) => new Ev(t, 6008, "EventLog", EventLogEntryType.Error);
        private static Ev OsStart12(DateTime t) => new Ev(t, 12, "Microsoft-Windows-Kernel-General");
        private static Ev Noise(DateTime t) => new Ev(t, 1, "Microsoft-Windows-FilterManager");

        [Fact]
        public void Real_Sequence_Sleep_Wake_Crash_Matches_The_Reference_Tool_Shape()
        {
            // Sequenza come sul PC reale: avvio, sospensione, risveglio, poi spegnimento senza evento e riavvio con 41/6008
            var boot = new DateTime(2026, 9, 5, 13, 40, 17);
            var sleep = boot.AddMinutes(37);                              // 14:17:34 ultimo evento
            var wake = new DateTime(2026, 9, 5, 18, 0, 0);
            var lastEvent = new DateTime(2026, 9, 5, 20, 20, 0);
            var os = new DateTime(2026, 9, 5, 20, 46, 29);
            var boot2 = new DateTime(2026, 9, 5, 20, 46, 47);
            var events = new IEventRecord[]
            {
                OsStart12(boot.AddSeconds(-14)), Boot6005(boot), Noise(boot.AddMinutes(5)),
                Sleep42(sleep),
                Wake1(wake), Noise(lastEvent),
                OsStart12(os), Crash41(os.AddSeconds(3)), Unexpected6008(boot2), Boot6005(boot2),
                Shutdown6006(boot2.AddHours(2))
            };

            var infos = new UsageLogTimeService(null).BuildUsageInfo(events).ToList();

            Assert.Equal(3, infos.Count);

            Assert.Equal(boot, infos[0].Interval.Start);
            Assert.Equal(IntervalEndKind.Sleep, infos[0].Interval.EndKind);
            Assert.Equal(sleep, infos[0].Interval.End);

            Assert.Equal(wake, infos[1].Interval.Start);                       // risveglio = nuovo intervallo
            Assert.Equal(IntervalEndKind.Unexpected, infos[1].Interval.EndKind);
            Assert.Equal(lastEvent, infos[1].Interval.End);                    // ultimo evento prima del successivo avvio OS
            Assert.Equal(lastEvent - wake, infos[1].Duration);

            Assert.Equal(boot2, infos[2].Interval.Start);
            Assert.True(infos[2].Interval.StartedAfterCrash);                  // 41 e 6008 vicini al 6005
            Assert.Equal(IntervalEndKind.Shutdown, infos[2].Interval.EndKind);
        }

        [Fact]
        public void Kernel_Power_41_Is_Not_An_Interval_Start()
        {
            var boot = new DateTime(2026, 10, 4, 14, 36, 34);
            var events = new IEventRecord[]
            {
                OsStart12(boot.AddSeconds(-30)), Crash41(boot.AddSeconds(-15)), Unexpected6008(boot), Boot6005(boot),
                Sleep42(boot.AddHours(1))
            };

            var infos = new UsageLogTimeService(null).BuildUsageInfo(events).ToList();

            var info = Assert.Single(infos);
            Assert.Equal(boot, info.Interval.Start);   // non 14:36:19 (orario del 41)
        }

        [Fact]
        public void Open_Interval_Duration_Uses_ObservedUntil_Or_Last_Event_But_Never_Now()
        {
            var boot = new DateTime(2020, 1, 1, 9, 0, 0);
            var events = new IEventRecord[] { Boot6005(boot), Noise(boot.AddHours(5)) };
            var service = new UsageLogTimeService(null);

            var fromImage = service.BuildUsageInfo(events).Single();                              // immagine: ultimo evento
            var live = service.BuildUsageInfo(events, boot.AddHours(8)).Single();                 // live: ora corrente indicata

            Assert.Equal(TimeSpan.FromHours(5), fromImage.Duration);
            Assert.Equal(TimeSpan.FromHours(8), live.Duration);
            Assert.Equal(IntervalEndKind.InProgress, fromImage.Interval.EndKind);
        }

        [Fact]
        public void Null_Events_Throw()
        {
            Assert.Throws<ArgumentNullException>(() => new UsageLogTimeService(null).BuildUsageInfo(null));
        }

        [Theory]
        [InlineData(IntervalEndKind.Shutdown, "Spegnimento")]
        [InlineData(IntervalEndKind.Sleep, "Sospensione")]
        [InlineData(IntervalEndKind.Unexpected, "Anomalo (stimato)")]
        [InlineData(IntervalEndKind.InProgress, "In corso")]
        public void End_Kind_Text(IntervalEndKind kind, string expected)
        {
            Assert.Equal(expected, IntervalEntry.ToText(kind));
        }
    }
}
