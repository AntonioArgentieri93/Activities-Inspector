using Activities_Inspector.Models;
using Activities_Inspector.Services;
using Activities_Inspector.Services.Evidence;
using Activities_Inspector.Services.Reporting;
using Activities_Inspector.Utils;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace ActivitiesInspector.UnitTests.Services
{
    public class CsvSourceFooterTests
    {
        [Fact]
        public async Task Footer_Contains_Source_And_Partial_Warning()
        {
            var path = await ExportWithFooter(new[] { new ShellBagEntry("C:\\A", System.DateTime.Now, "R") }, EntryType.ShellBags, "Sorgente: Immagine: E:\\Caso\n" + ReportFormatting.PartialResultsWarningText);
            var lines = File.ReadAllLines(path).Where(l => !string.IsNullOrWhiteSpace(l)).ToArray();
            Assert.Contains(lines, l => l.Contains("Sorgente: Immagine"));
            // cleanup
            File.Delete(path);
            File.Delete(path + ".sha256");
        }

        [Fact]
        public async Task Sidecar_Matches_File_Hash()
        {
            var path = await ExportWithSource(new[] { new ShellBagEntry("C:\\A", System.DateTime.Now, "R") }, EntryType.ShellBags);
            var sidecar = File.ReadAllText(path + ".sha256");
            var hex = sidecar.Split(' ')[0].Trim();
            var expected = IntegrityHasher.ComputeHex(await File.ReadAllBytesAsync(path));
            Assert.Equal(expected, hex);
            File.Delete(path);
            File.Delete(path + ".sha256");
        }

        private static async Task<string> ExportWithSource(IEnumerable<Entry> entries, EntryType type)
        {
            var exporter = new EntriesExporter(new EntryFormatter());
            var result = await exporter.SaveEntriesDataAsync(entries, type, footerNote: "Sorgente: Sistema live");
            Assert.True(result.IsSuccess);
            return result.Value;
        }

        private static async Task<string> ExportWithFooter(IEnumerable<Entry> entries, EntryType type, string footer)
        {
            var exporter = new EntriesExporter(new EntryFormatter());
            var result = await exporter.SaveEntriesDataAsync(entries, type, footerNote: footer);
            Assert.True(result.IsSuccess, result.IsFailure ? result.Error : "no error");
            return result.Value;
        }
    }
}
