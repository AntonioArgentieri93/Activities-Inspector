using Activities_Inspector.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace ActivitiesInspector.UnitTests.Services
{
    public class TimeIntervalsPairingTests
    {
        private static readonly DateTime T0 = new DateTime(2024, 1, 1, 8, 0, 0);
        private static readonly DateTime T1 = new DateTime(2024, 1, 1, 12, 0, 0);
        private static readonly DateTime T2 = new DateTime(2024, 1, 1, 18, 0, 0);

        [Fact]
        public void Pair_Gets_Machine_Of_Matched_Start()
        {
            // Scenario del bug: due boot su macchine diverse, la chiusura
            // deve portare il nome della macchina che l'ha aperta.
            var points = new List<(DateTime Time, string Machine, bool IsStart)>
            {
                (T0, "M1", true),
                (T1, "M2", true),
                (T2, "M2", false)
            };

            var pairs = UsageLogTimeService.PairIntervals(points);

            Assert.Equal(2, pairs.Count);
            Assert.Equal(T1, pairs[0].Interval.Start);
            Assert.Equal(T2, pairs[0].Interval.End);
            Assert.Equal("M2", pairs[0].MachineName);
        }

        [Fact]
        public void Open_Interval_Keeps_Last_Start_Machine()
        {
            var points = new List<(DateTime Time, string Machine, bool IsStart)>
            {
                (T0, "M1", true)
            };

            var pairs = UsageLogTimeService.PairIntervals(points);

            var open = Assert.Single(pairs);
            Assert.Equal(T0, open.Interval.Start);
            Assert.Null(open.Interval.End);
            Assert.Equal("M1", open.MachineName);
        }

        [Fact]
        public void Unsorted_Input_Pairs_By_Time()
        {
            var points = new List<(DateTime Time, string Machine, bool IsStart)>
            {
                (T2, "M1", false),
                (T1, "M1", true),
                (T0, "M1", true)
            };

            var pairs = UsageLogTimeService.PairIntervals(points);

            Assert.Equal(T1, pairs[0].Interval.Start);
            Assert.Equal(T2, pairs[0].Interval.End);
        }

        [Fact]
        public void Empty_Input_Yields_Filterable_Sentinel()
        {
            var pairs = UsageLogTimeService.PairIntervals(
                new List<(DateTime Time, string Machine, bool IsStart)>());

            var single = Assert.Single(pairs);
            Assert.Equal(DateTime.MinValue, single.Interval.Start);
        }

        [Fact]
        public void End_Without_Start_Produces_No_Pair()
        {
            var points = new List<(DateTime Time, string Machine, bool IsStart)>
            {
                (T2, "M1", false)
            };

            var pairs = UsageLogTimeService.PairIntervals(points)
                .Where(p => p.Interval.Start != DateTime.MinValue)
                .ToList();

            Assert.Empty(pairs);
        }
    }
}
