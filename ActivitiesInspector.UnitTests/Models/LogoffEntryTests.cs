using Activities_Inspector.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace ActivitiesInspector.UnitTests.Models
{
    public class LogoffEntryTests
    {
        [Fact]
        public void SameIndex_AreEqual_WithSameHashCode()
        {
            var first = new LogoffEntry("0xAAA", new DateTime(2024, 5, 1, 10, 0, 0));
            var second = new LogoffEntry("0xAAA", new DateTime(2024, 5, 1, 12, 0, 0));

            Assert.True(first.Equals(second));
            Assert.Equal(first.GetHashCode(), second.GetHashCode());
        }

        [Fact]
        public void DifferentIndex_AreNotEqual()
        {
            var first = new LogoffEntry("0xAAA", new DateTime(2024, 5, 1, 10, 0, 0));
            var second = new LogoffEntry("0xBBB", new DateTime(2024, 5, 1, 10, 0, 0));

            Assert.False(first.Equals(second));
        }

        [Fact]
        public void Distinct_Removes_DuplicateIndexes()
        {
            var entries = new List<LogoffEntry>
            {
                new LogoffEntry("0xAAA", new DateTime(2024, 5, 1, 10, 0, 0)),
                new LogoffEntry("0xBBB", new DateTime(2024, 5, 1, 11, 0, 0)),
                new LogoffEntry("0xAAA", new DateTime(2024, 5, 1, 10, 0, 0))
            };

            Assert.Equal(2, entries.Distinct().Count());
        }

        [Fact]
        public void Equals_Null_And_ForeignType_ReturnFalse()
        {
            var entry = new LogoffEntry("0xAAA", new DateTime(2024, 5, 1, 10, 0, 0));

            Assert.False(entry.Equals(null));
            Assert.False(entry.Equals("not-a-logoff-entry"));
        }
    }
}
