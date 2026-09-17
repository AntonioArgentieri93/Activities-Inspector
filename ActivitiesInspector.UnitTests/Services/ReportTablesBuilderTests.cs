using Activities_Inspector.Models;
using Activities_Inspector.Services.Reporting;
using MigraDocCore.DocumentObjectModel;
using MigraDocCore.DocumentObjectModel.Tables;
using System;
using System.Linq;
using Xunit;

namespace ActivitiesInspector.UnitTests.Services
{
    public class ReportTablesBuilderTests
    {
        private static Table BuildTable(Action<Section> build)
        {
            var document = new Document();
            var section = document.AddSection();
            build(section);
            return Assert.Single(section.Elements.OfType<Table>());
        }

        [Fact]
        public void UsageInfos_Header_Plus_Rows()
        {
            var infos = new[]
            {
                new UsageInfo(
                    new IntervalEntry(new DateTime(2024, 1, 15, 8, 0, 0), new DateTime(2024, 1, 15, 18, 0, 0)),
                    TimeSpan.FromHours(10), "PC"),
                new UsageInfo(
                    new IntervalEntry(new DateTime(2024, 1, 16, 8, 0, 0), null),
                    TimeSpan.FromHours(2), "PC")
            };

            var table = BuildTable(s => ReportTablesBuilder.AddUsageInfos(infos, s));

            Assert.Equal(3, table.Rows.Count);
            Assert.Equal(5, table.Rows[0].Cells.Count);
            Assert.Equal(5, table.Rows[1].Cells.Count);
        }

        [Fact]
        public void InstalledPrograms_Header_Plus_Rows()
        {
            var entries = new[]
            {
                new InstallEntry("App", "HKLM", "C:\\App", new DateTime(2024, 1, 10)),
                new InstallEntry("Tool", "HKCU", "C:\\Tool", null)
            };

            var table = BuildTable(s => ReportTablesBuilder.AddInstalledPrograms(entries, s));

            Assert.Equal(3, table.Rows.Count);
            Assert.Equal(4, table.Rows[0].Cells.Count);
            Assert.Equal(4, table.Rows[1].Cells.Count);
        }

        [Fact]
        public void RecentFolder_Header_Plus_Rows()
        {
            var entries = new[]
            {
                new RecentFolderEntry(new DateTime(2024, 1, 15), "a.txt", "s", "C:\\a.txt"),
                new RecentFolderEntry(new DateTime(2024, 1, 16), "b.txt", "s", "C:\\b.txt")
            };

            var table = BuildTable(s => ReportTablesBuilder.AddRecentFolderEntries(entries, s));

            Assert.Equal(3, table.Rows.Count);
            Assert.Equal(5, table.Rows[0].Cells.Count);
            Assert.Equal(5, table.Rows[1].Cells.Count);
        }

        [Fact]
        public void Prefetch_Header_Plus_Rows()
        {
            var entries = new[]
            {
                new PrefetchInfoEntry("A.EXE", "A.pf", new DateTime(2024, 1, 15), ".EXE"),
                new PrefetchInfoEntry("B.EXE", "B.pf", new DateTime(2024, 1, 16), ".EXE")
            };

            var table = BuildTable(s => ReportTablesBuilder.AddPrefetchInfoEntries(entries, s));

            Assert.Equal(3, table.Rows.Count);
            Assert.Equal(6, table.Rows[0].Cells.Count);
            Assert.Equal(6, table.Rows[1].Cells.Count);
        }

        [Fact]
        public void ShellBags_Header_Plus_Rows()
        {
            var entries = new[]
            {
                new ShellBagEntry("C:\\A", new DateTime(2024, 1, 15), "R1"),
                new ShellBagEntry("C:\\B", new DateTime(2024, 1, 16), "R2")
            };

            var table = BuildTable(s => ReportTablesBuilder.AddShellbagsEntries(entries, s));

            Assert.Equal(3, table.Rows.Count);
            Assert.Equal(3, table.Rows[0].Cells.Count);
            Assert.Equal(3, table.Rows[1].Cells.Count);
        }

        [Fact]
        public void Sessions_Header_Plus_Rows()
        {
            var entries = new[]
            {
                new SessionEntry("0x1", "u", "g", "m",
                    new DateTime(2024, 1, 15, 8, 0, 0), new DateTime(2024, 1, 15, 9, 0, 0),
                    TimeSpan.FromHours(1), "10.0.0.1", "2"),
                new SessionEntry("0x2", "u", "g", "m",
                    new DateTime(2024, 1, 16, 8, 0, 0), null, null, string.Empty, string.Empty)
            };

            var table = BuildTable(s => ReportTablesBuilder.AddSessionEntries(entries, s));

            Assert.Equal(3, table.Rows.Count);
            Assert.Equal(10, table.Rows[0].Cells.Count);
            Assert.Equal(10, table.Rows[1].Cells.Count);
        }

        [Fact]
        public void SystemTimeChanged_Header_Plus_Rows()
        {
            var entries = new[]
            {
                new SystemTimeChangedEntry("u", "t", "old", "new")
            };

            var table = BuildTable(s => ReportTablesBuilder.AddSystemTimeChangedEntries(entries, s));

            Assert.Equal(2, table.Rows.Count);
            Assert.Equal(4, table.Rows[0].Cells.Count);
            Assert.Equal(4, table.Rows[1].Cells.Count);
        }

        [Fact]
        public void Usb_Header_Plus_Rows()
        {
            var entries = new[]
            {
                new UsbEntry(true, "D1", "S1", "0781", "5567", "Mass", null, null),
                new UsbEntry(false, "D2", "S2", "0781", "5581", "Mass", null, null)
            };

            var table = BuildTable(s => ReportTablesBuilder.AddUsbEntries(entries, s));

            Assert.Equal(3, table.Rows.Count);
            Assert.Equal(8, table.Rows[0].Cells.Count);
            Assert.Equal(8, table.Rows[1].Cells.Count);
        }

        [Fact]
        public void Empty_Data_Adds_No_Table()
        {
            var document = new Document();
            var section = document.AddSection();
            ReportTablesBuilder.AddUsageInfos(new UsageInfo[0], section);

            Assert.Empty(section.Elements.OfType<Table>());
        }
    }
}
