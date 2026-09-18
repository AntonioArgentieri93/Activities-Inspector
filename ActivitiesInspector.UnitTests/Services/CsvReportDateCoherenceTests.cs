using Activities_Inspector.Models;
using Activities_Inspector.Services;
using Activities_Inspector.Services.Reporting;
using Activities_Inspector.Utils;
using MigraDocCore.DocumentObjectModel;
using MigraDocCore.DocumentObjectModel.Tables;
using System;
using System.Linq;
using System.Text;
using Xunit;

namespace ActivitiesInspector.UnitTests.Services
{
    public class CsvReportDateCoherenceTests
    {
        private readonly EntryFormatter _formatter = new EntryFormatter();

        private static string TableText(Section section)
        {
            var table = Assert.Single(section.Elements.OfType<Table>());
            var sb = new StringBuilder();
            foreach (Row row in table.Rows)
                foreach (Cell cell in row.Cells)
                    foreach (var paragraph in cell.Elements.OfType<Paragraph>())
                        sb.Append(string.Concat(paragraph.Elements.OfType<Text>().Select(t => t.Content)));
            return sb.ToString().Replace("\u200B", string.Empty);
        }

        private static Section BuildReport(Action<Section> build)
        {
            var document = new Document();
            var section = document.AddSection();
            build(section);
            return section;
        }

        private static void AssertSameDates(string csvRow, string reportText, params string[] expectedDates)
        {
            foreach (var date in expectedDates)
            {
                Assert.Contains(date, csvRow);
                Assert.Contains(date, reportText);
            }
        }

        [Fact]
        public void Usage_Dates_Match()
        {
            var start = new DateTime(2024, 1, 15, 8, 0, 0);
            var end = new DateTime(2024, 1, 15, 18, 0, 0);
            var entry = new UsageInfo(new IntervalEntry(start, end), TimeSpan.FromHours(10), "PC");

            var csv = _formatter.AsCsv(entry);
            var report = TableText(BuildReport(s => ReportTablesBuilder.AddUsageInfos(new[] { entry }, s)));

            AssertSameDates(csv, report,
                DateBuilder.BuildFromDateTime(start), DateBuilder.BuildFromDateTime(end));
        }

        [Fact]
        public void Install_Date_Matches()
        {
            var date = new DateTime(2024, 1, 10);
            var entry = new InstallEntry("App", "HKLM", @"C:\App", date);

            var csv = _formatter.AsCsv(entry);
            var report = TableText(BuildReport(s => ReportTablesBuilder.AddInstalledPrograms(new[] { entry }, s)));

            AssertSameDates(csv, report, DateBuilder.BuildFromDateTime(date));
        }

        [Fact]
        public void Recent_ActionTime_Matches()
        {
            var action = new DateTime(2024, 1, 15);
            var entry = new RecentFolderEntry(action, "a.txt", "s", @"C:\a.txt");

            var csv = _formatter.AsCsv(entry);
            var report = TableText(BuildReport(s => ReportTablesBuilder.AddRecentFolderEntries(new[] { entry }, s)));

            AssertSameDates(csv, report, DateBuilder.BuildFromDateTime(action));
        }

        [Fact]
        public void Prefetch_Dates_Match()
        {
            var last = new DateTime(2024, 1, 15);
            var first = new DateTime(2024, 1, 10);
            var entry = new PrefetchInfoEntry("A.EXE", "A.pf", last, ".EXE") { FirstRunTime = first, RunCount = 3 };

            var csv = _formatter.AsCsv(entry);
            var report = TableText(BuildReport(s => ReportTablesBuilder.AddPrefetchInfoEntries(new[] { entry }, s)));

            AssertSameDates(csv, report,
                DateBuilder.BuildFromDateTime(last), DateBuilder.BuildFromDateTime(first));
        }

        [Fact]
        public void ShellBag_Date_Matches()
        {
            var date = new DateTime(2024, 1, 15);
            var entry = new ShellBagEntry(@"C:\A", date, "R1");

            var csv = _formatter.AsCsv(entry);
            var report = TableText(BuildReport(s => ReportTablesBuilder.AddShellbagsEntries(new[] { entry }, s)));

            AssertSameDates(csv, report, DateBuilder.BuildFromDateTime(date));
        }

        [Fact]
        public void Session_Dates_Match()
        {
            var logOn = new DateTime(2024, 1, 15, 8, 0, 0);
            var logOff = new DateTime(2024, 1, 15, 9, 0, 0);
            var entry = new SessionEntry("0x1", "u", "g", "m", logOn, logOff, TimeSpan.FromHours(1), "10.0.0.1", "2");

            var csv = _formatter.AsCsv(entry);
            var report = TableText(BuildReport(s => ReportTablesBuilder.AddSessionEntries(new[] { entry }, s)));

            AssertSameDates(csv, report,
                DateBuilder.BuildFromDateTime(logOn), DateBuilder.BuildFromDateTime(logOff));
        }

        [Fact]
        public void SystemTimeChanged_Strings_Match()
        {
            var entry = new SystemTimeChangedEntry("u", "t", "old", "new");

            var csv = _formatter.AsCsv(entry);
            var report = TableText(BuildReport(s => ReportTablesBuilder.AddSystemTimeChangedEntries(new[] { entry }, s)));

            Assert.Contains("u ; t ; old ; new", csv);
            foreach (var token in new[] { "u", "t", "old", "new" })
                Assert.Contains(token, report);
        }

        [Fact]
        public void Usb_Dates_Match()
        {
            var connected = new DateTimeOffset(new DateTime(2024, 1, 15));
            var entry = new UsbEntry(true, "D1", "S1", "0781", "5567", "Mass", connected, null);

            var csv = _formatter.AsCsv(entry);
            var report = TableText(BuildReport(s => ReportTablesBuilder.AddUsbEntries(new[] { entry }, s)));

            AssertSameDates(csv, report, DateBuilder.BuildFromDateTime(connected.LocalDateTime));
        }
    }
}
