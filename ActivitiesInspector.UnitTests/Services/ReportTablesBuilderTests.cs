using Activities_Inspector.Models;
using Activities_Inspector.Services;
using Activities_Inspector.Services.Reporting;
using MigraDocCore.DocumentObjectModel;
using MigraDocCore.DocumentObjectModel.Fields;
using MigraDocCore.DocumentObjectModel.Tables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        public void Empty_Data_Adds_Explicit_Note_And_No_Table()
        {
            var document = new Document();
            var section = document.AddSection();
            ReportTablesBuilder.AddUsageInfos(new UsageInfo[0], section);

            Assert.Empty(section.Elements.OfType<Table>());
            Assert.Contains(ReportFormatting.NoResultsNoteText, ParagraphTexts(section));
        }

        [Fact]
        public void All_Sections_Present_Even_When_Empty()
        {
            var document = new Document();
            var section = document.AddSection();
            ReportTablesBuilder.AddContents(
                new UsageInfo[0], new InstallEntry[0], new RecentFolderEntry[0], new PrefetchInfoEntry[0],
                new ShellBagEntry[0], new SessionEntry[0], new SystemTimeChangedEntry[0], new UsbEntry[0], section);

            Assert.Empty(section.Elements.OfType<Table>());
            Assert.Equal(ReportSectionCatalog.All.Length - 1, ParagraphTexts(section).Count(t => t == ReportFormatting.NoResultsNoteText));
            Assert.Contains("Nessun artefatto acquisito: eseguire le funzionalita' prima di generare il report.", ParagraphTexts(section));
            Assert.Equal(
                ReportSectionCatalog.All.Select(e => e.Key).OrderBy(k => k),
                section.Elements.OfType<Paragraph>().SelectMany(p => p.Elements.OfType<BookmarkField>()).Select(b => b.Name).OrderBy(n => n));
        }

        [Fact]
        public void Header_Row_Repeats_On_Each_Page()
        {
            var infos = new[]
            {
                new UsageInfo(
                    new IntervalEntry(new DateTime(2024, 1, 15, 8, 0, 0), new DateTime(2024, 1, 15, 18, 0, 0)),
                    TimeSpan.FromHours(10), "PC")
            };

            var table = BuildTable(s => ReportTablesBuilder.AddUsageInfos(infos, s));

            Assert.True(table.Rows[0].HeadingFormat);
        }

        [Fact]
        public void Table_Of_Contents_Lists_All_Sections_With_Page_References()
        {
            var document = new Document();
            var section = document.AddSection();
            new ReportCoverBuilder(new FakeNetService()).AddTableOfContents(section);

            var expectedKeys = ReportSectionCatalog.All.Select(e => e.Key).OrderBy(k => k).ToArray();
            var paragraphs = section.Elements.OfType<Paragraph>().ToArray();

            Assert.Equal(
                expectedKeys,
                paragraphs.SelectMany(p => p.Elements.OfType<Hyperlink>()).Select(h => h.Name).OrderBy(n => n));
            Assert.Equal(
                expectedKeys,
                paragraphs.SelectMany(p => p.Elements.OfType<PageRefField>()).Select(f => f.Name).OrderBy(n => n));
        }

        [Fact]
        public void Footer_Contains_Page_Numbers()
        {
            var document = new Document();
            var section = document.AddSection();
            ReportFormatting.AddFooterWithPageNumbers(section);

            Assert.NotEmpty(section.Footers.Primary.Elements);
        }

        [Fact]
        public void Partial_ShellBags_Adds_Warning_Before_Table()
        {
            var entries = new[]
            {
                new ShellBagEntry("C:\\A", new DateTime(2024, 1, 15), "R1")
            };

            var document = new Document();
            var section = document.AddSection();
            ReportTablesBuilder.AddShellbagsEntries(entries, section, isPartial: true);

            Assert.Single(section.Elements.OfType<Table>());
            Assert.Contains(ReportFormatting.PartialResultsWarningText, ParagraphTexts(section));
        }

        [Fact]
        public void Complete_ShellBags_Adds_No_Warning()
        {
            var entries = new[]
            {
                new ShellBagEntry("C:\\A", new DateTime(2024, 1, 15), "R1")
            };

            var document = new Document();
            var section = document.AddSection();
            ReportTablesBuilder.AddShellbagsEntries(entries, section);

            Assert.DoesNotContain(ReportFormatting.PartialResultsWarningText, ParagraphTexts(section));
        }

        [Fact]
        public void Integrity_Manifest_Renders_All_Row_Kinds()
        {
            var utc = new DateTime(2024, 1, 15, 10, 30, 0);
            var records = new[]
            {
                new IntegrityRecord(EntryType.Prefetch, @"C:\Windows\Prefetch\A.pf",
                    "ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad", 128, utc, IntegrityStatus.Acquired),
                new IntegrityRecord(EntryType.Usb, @"C:\Windows\System32\config\SYSTEM",
                    null, null, utc, IntegrityStatus.NotAcquirable, "Accesso negato"),
                IntegrityRecord.LiveSource(EntryType.InstalledPrograms, @"HKLM\Software (registro live)")
            };

            var document = new Document();
            var section = document.AddSection();
            ReportTablesBuilder.AddIntegrityManifest(records, section);

            var table = Assert.Single(section.Elements.OfType<Table>());
            Assert.Equal(4, table.Rows.Count);
            var texts = TableText(table);
            Assert.Contains("ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad", texts);
            Assert.Contains("Non acquisibile (Accesso negato)", texts);
            Assert.Contains("Letto via API live, nessun file acquisibile", texts);
            Assert.Contains("15/1/2024 10:30:00 UTC", texts);
            Assert.Contains("Prefetch", texts);
        }

        [Fact]
        public void Empty_Integrity_Manifest_Adds_Explicit_Note_And_No_Table()
        {
            var document = new Document();
            var section = document.AddSection();
            ReportTablesBuilder.AddIntegrityManifest(new IntegrityRecord[0], section);

            Assert.Empty(section.Elements.OfType<Table>());
            Assert.NotEmpty(section.Elements.OfType<Paragraph>());
        }

        private static string TableText(Table table)
        {
            var sb = new StringBuilder();
            foreach (Row row in table.Rows)
                foreach (Cell cell in row.Cells)
                    foreach (var paragraph in cell.Elements.OfType<Paragraph>())
                        sb.Append(string.Concat(paragraph.Elements.OfType<Text>().Select(t => t.Content)));
            return sb.ToString().Replace("\u200B", string.Empty);
        }

        private static List<string> ParagraphTexts(Section section)
        {
            return section.Elements.OfType<Paragraph>()
                .Select(p => string.Concat(p.Elements.OfType<Text>().Select(t => t.Content)).Replace("\u200B", string.Empty))
                .ToList();
        }

        private sealed class FakeNetService : INetService
        {
            public IEnumerable<string> GetAvailablePrivateIPs() => Enumerable.Empty<string>();
            public string GetPublicIPAddress() => string.Empty;
        }
    }
}
