using Activities_Inspector.Models;
using Activities_Inspector.Services;
using Activities_Inspector.Utils;
using System;
using Xunit;

namespace ActivitiesInspector.UnitTests.Services
{
    public class EntryFormatterTests
    {
        private readonly EntryFormatter _formatter = new EntryFormatter();

        [Fact]
        public void Session_Missing_LogOn_Renders_MinValue_Date()
        {
            var entry = new SessionEntry("0x5", "u", "g", "m", DateTime.MinValue,
                new DateTime(2024, 1, 16, 9, 0, 0), null, string.Empty, string.Empty);

            var csv = _formatter.AsCsv(entry);

            Assert.Contains(DateBuilder.BuildFromDateTime(DateTime.MinValue), csv);
        }

        [Fact]
        public void Session_Full_Row_Uses_Shared_Date_Format()
        {
            var logOn = new DateTime(2024, 1, 15, 8, 0, 0);
            var logOff = new DateTime(2024, 1, 15, 9, 0, 0);
            var entry = new SessionEntry("0x1", "u", "g", "m", logOn, logOff,
                TimeSpan.FromHours(1), "10.0.0.1", "2");

            var expected = string.Format("u ; g ; m ; {0} ; {1} ; 0 giorno/i - 1 ora/e - 0 minuti - 0 secondi. ; 10.0.0.1 ; Interactive (2) ;  ; 0x1",
                DateBuilder.BuildFromDateTime(logOn), DateBuilder.BuildFromDateTime(logOff));

            Assert.Equal(expected, _formatter.AsCsv(entry));
        }

        [Fact]
        public void Usage_Open_Interval_Leaves_End_Empty()
        {
            var start = new DateTime(2024, 1, 16, 8, 0, 0);
            var entry = new UsageInfo(new IntervalEntry(start, null), TimeSpan.Zero, "PC");

            var expected = string.Format("{0} ;  ; 0 giorno/i - 0 ora/e - 0 minuti - 0 secondi. ; PC ; No",
                DateBuilder.BuildFromDateTime(start));

            Assert.Equal(expected, _formatter.AsCsv(entry));
        }

        [Fact]
        public void Install_Date_Uses_Shared_Format_Or_Empty()
        {
            var date = new DateTime(2024, 1, 10);
            var withDate = new InstallEntry("App", "HKLM", @"C:\App", date);
            var withoutDate = new InstallEntry("Tool", "HKCU", @"C:\Tool", null);

            Assert.Equal($"App ; HKLM ; C:\\App ; {DateBuilder.BuildFromDateTime(date)}",
                _formatter.AsCsv(withDate));
            Assert.Equal("Tool ; HKCU ; C:\\Tool ; ", _formatter.AsCsv(withoutDate));
        }
    }
}
