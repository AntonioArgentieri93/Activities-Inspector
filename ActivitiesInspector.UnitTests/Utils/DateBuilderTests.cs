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
            // Il formato dell'app è "dd/MM/yyyy": giorno e mese sempre a due cifre.
            Assert.Equal("15/01/2024", DateBuilder.BuildFromString("20240115"));
        }

        // --- Formato: giorno e mese sempre a due cifre (prima "04/10/2026" ma "20/9/2026") ---

        [Theory]
        [InlineData(2026, 9, 20, "20/09/2026")]
        [InlineData(2026, 10, 4, "04/10/2026")]
        [InlineData(2026, 1, 1, "01/01/2026")]
        [InlineData(2026, 12, 31, "31/12/2026")]
        public void Date_Parts_Are_Always_Two_Digits(int y, int m, int d, string expectedDatePart)
        {
            var text = DateBuilder.BuildFromDateTime(new DateTime(y, m, d, 8, 5, 3));

            Assert.StartsWith(expectedDatePart + " 08:05:03 GMT", text);
        }

        [Fact]
        public void Format_Does_Not_Depend_On_The_Windows_Language()
        {
            // Il "/" di un formato personalizzato e' il separatore della cultura: con de-DE darebbe "04.10.2026"
            var previous = System.Globalization.CultureInfo.CurrentCulture;
            try
            {
                System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("de-DE");

                Assert.StartsWith("04/10/2026 14:36:51 GMT", DateBuilder.BuildFromDateTime(new DateTime(2026, 10, 4, 14, 36, 51)));
                Assert.Equal("15/01/2024", DateBuilder.BuildFromString("20240115"));
            }
            finally
            {
                System.Globalization.CultureInfo.CurrentCulture = previous;
            }
        }

        [Theory]
        [InlineData(0, 0, "GMT+0")]
        [InlineData(2, 0, "GMT+2")]
        [InlineData(1, 0, "GMT+1")]
        [InlineData(5, 30, "GMT+5:30")]       // India: prima "GMT+5"
        [InlineData(-5, 0, "GMT-5")]          // New York: prima "GMT+-5"
        [InlineData(-3, -30, "GMT-3:30")]     // Terranova
        [InlineData(12, 45, "GMT+12:45")]     // Chatham
        public void Offset_Has_The_Right_Sign_And_Minutes(int hours, int minutes, string expected)
        {
            Assert.Equal(expected, DateBuilder.FormatOffset(new TimeSpan(hours, minutes, 0)));
        }

        [Theory]
        [InlineData("05/10/2026", "05/10/2026")]
        [InlineData("5/10/2026", "05/10/2026")]            // vecchio formato
        [InlineData("20/9/2026", "20/09/2026")]            // vecchio formato (mese senza zero)
        public void BuildFromString_Accepts_Old_And_New_Date_Formats(string input, string expected)
        {
            Assert.Equal(expected, DateBuilder.BuildFromString(input));
        }

        [Theory]
        [InlineData("20/9/2026 08:05:03")]
        [InlineData("20/09/2026 08:05:03")]
        public void BuildFromString_Accepts_Old_And_New_DateTime_Formats(string input)
        {
            Assert.StartsWith("20/09/2026 08:05:03 GMT", DateBuilder.BuildFromString(input));
        }

        [Fact]
        public void BuildDateTimeFromString_Accepts_Old_And_New_Date_Formats()
        {
            Assert.Equal(new DateTime(2026, 9, 20), DateBuilder.BuildDateTimeFromString("20/9/2026"));
            Assert.Equal(new DateTime(2026, 9, 20), DateBuilder.BuildDateTimeFromString("20/09/2026"));
        }

        [Fact]
        public void Formatted_Date_Strings_Of_Different_Months_Are_Not_Chronologically_Sortable_So_Models_Keep_DateTimes()
        {
            // Documenta il motivo della scelta di SystemTimeChangedEntry: il testo gg/mm/aaaa ordina per giorno
            var october = DateBuilder.BuildFromDateTime(new DateTime(2026, 10, 5));
            var september = DateBuilder.BuildFromDateTime(new DateTime(2026, 9, 20));

            Assert.True(string.CompareOrdinal(october, september) < 0);   // il 5 ottobre "precede" il 20 settembre come testo
            Assert.True(new DateTime(2026, 10, 5) > new DateTime(2026, 9, 20));
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
                "15/01/2024 10:30:00 " + DateBuilder.FormatOffset(TimeZoneInfo.Local.GetUtcOffset(winter)),
                DateBuilder.BuildFromDateTime(winter));
            Assert.Equal(
                "15/07/2024 10:30:00 " + DateBuilder.FormatOffset(TimeZoneInfo.Local.GetUtcOffset(summer)),
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
    }
}
