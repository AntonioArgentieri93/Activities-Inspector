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
        public void BuildFromDateTime_Includes_Offset()
        {
            var offset = TimeZoneInfo.Local.GetUtcOffset(DateTime.UtcNow);

            Assert.Equal(
                "15/1/2024 10:30:00 GMT" + (offset.Hours >= 0 ? "+" : string.Empty) + offset.Hours,
                DateBuilder.BuildFromDateTime(new DateTime(2024, 1, 15, 10, 30, 0)));
        }
    }
}
