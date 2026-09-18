using Activities_Inspector.Utils;
using System;
using Xunit;

namespace ActivitiesInspector.UnitTests.Utils
{
    public class DateBuilderTests
    {
        [Fact]
        public void BuildFromString_YYYYMMDD()
        {
            // Nota: il formato dell'app è "dd/M/yyyy" (mese senza zero).
            Assert.Equal("15/1/2024", DateBuilder.BuildFromString("20240115"));
        }

        [Fact]
        public void BuildFromString_Null_Or_Empty()
        {
            Assert.Equal(string.Empty, DateBuilder.BuildFromString(null));
            Assert.Equal(string.Empty, DateBuilder.BuildFromString(string.Empty));
        }

        [Fact]
        public void BuildDateTimeFromString_YYYYMMDD()
        {
            var result = DateBuilder.BuildDateTimeFromString("20240115");

            Assert.NotNull(result);
            Assert.Equal(new DateTime(2024, 1, 15), result.Value.Date);
        }

        [Fact]
        public void BuildFromDateTime_Uses_Offset_Of_The_Date_Not_Of_Now()
        {
            var winter = new DateTime(2024, 1, 15, 10, 30, 0);
            var summer = new DateTime(2024, 7, 15, 10, 30, 0);

            Assert.Equal(
                "15/1/2024 10:30:00 GMT" + FormatOffset(TimeZoneInfo.Local.GetUtcOffset(winter)),
                DateBuilder.BuildFromDateTime(winter));
            Assert.Equal(
                "15/7/2024 10:30:00 GMT" + FormatOffset(TimeZoneInfo.Local.GetUtcOffset(summer)),
                DateBuilder.BuildFromDateTime(summer));
        }

        [Fact]
        public void ToLocal_Matches_ToLocalTime_For_Every_Kind()
        {
            var utc = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc);
            var local = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Local);
            var unspecified = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Unspecified);

            Assert.Equal(utc.ToLocalTime(), DateBuilder.ToLocal(utc));
            Assert.Equal(local.ToLocalTime(), DateBuilder.ToLocal(local));
            Assert.Equal(unspecified.ToLocalTime(), DateBuilder.ToLocal(unspecified));
        }

        private static string FormatOffset(TimeSpan offset)
            => (offset.Hours >= 0 ? "+" : string.Empty) + offset.Hours;

        [Fact]
        public void BuildFromDateTimeUtc_Uses_Utc_Label()
        {
            Assert.Equal(
                "15/1/2024 10:30:00 UTC",
                DateBuilder.BuildFromDateTimeUtc(new DateTime(2024, 1, 15, 10, 30, 0)));
        }
    }
}
