using Activities_Inspector.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace ActivitiesInspector.UnitTests.Models
{
    public class LogOnEntryTests
    {
        private static LogOnEntry Make(string index, DateTime time, string user)
        {
            return new LogOnEntry(4624, "PC", index, time, user, "DOM", "G", 2, "1.2.3.4");
        }

        [Fact]
        public void SameTimestamp_DifferentIndex_AreNotEqual()
        {
            var time = new DateTime(2024, 5, 1, 10, 0, 0);

            var first = Make("0xAAA", time, "alice");
            var second = Make("0xBBB", time, "bob");

            Assert.False(first.Equals(second));
        }

        [Fact]
        public void Distinct_Keeps_DifferentLogons_WithSameTimestamp()
        {
            var time = new DateTime(2024, 5, 1, 10, 0, 0);
            var entries = new List<LogOnEntry>
            {
                Make("0xAAA", time, "alice"),
                Make("0xBBB", time, "bob")
            };

            Assert.Equal(2, entries.Distinct().Count());
        }

        [Fact]
        public void SameIndex_AreEqual_WithSameHashCode()
        {
            var time = new DateTime(2024, 5, 1, 10, 0, 0);
            var first = Make("0xAAA", time, "alice");
            var second = Make("0xAAA", time, "alice");

            Assert.True(first.Equals(second));
            Assert.Equal(first.GetHashCode(), second.GetHashCode());
        }

        [Fact]
        public void Distinct_Removes_DuplicateIndexes_Anywhere()
        {
            var time = new DateTime(2024, 5, 1, 10, 0, 0);
            var entries = new List<LogOnEntry>
            {
                Make("0xAAA", time, "alice"),
                Make("0xBBB", time.AddHours(1), "bob"),
                Make("0xAAA", time, "alice")
            };

            Assert.Equal(2, entries.Distinct().Count());
        }

        [Fact]
        public void Equals_Null_And_ForeignType_ReturnFalse()
        {
            var entry = Make("0xAAA", new DateTime(2024, 5, 1, 10, 0, 0), "alice");

            Assert.False(entry.Equals(null));
            Assert.False(entry.Equals("not-a-logon-entry"));
        }
    }
}
