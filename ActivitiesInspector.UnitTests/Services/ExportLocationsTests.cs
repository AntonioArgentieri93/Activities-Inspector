using Activities_Inspector.Models;
using Activities_Inspector.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace ActivitiesInspector.UnitTests.Services
{
    public class ExportLocationsTests
    {
        [Fact]
        public void OutputDirectory_Ends_With_Output()
        {
            Assert.EndsWith("Output", ExportLocations.OutputDirectory().TrimEnd(Path.DirectorySeparatorChar));
        }

        [Fact]
        public void Local_Path_Is_Not_Removable()
        {
            Assert.False(ExportLocations.IsRemovable(Path.GetTempPath()));
            Assert.False(ExportLocations.IsRemovable(AppDomain.CurrentDomain.BaseDirectory));
            Assert.False(ExportLocations.IsRemovable(null));
            Assert.False(ExportLocations.IsRemovable(string.Empty));
        }

        [Fact]
        public async Task SaveEntries_Returns_Created_File_Path()
        {
            var exporter = new EntriesExporter(new FakeFormatter());

            var result = await exporter.SaveEntriesDataAsync(new List<Entry>(), EntryType.ShellBags);

            try
            {
                Assert.True(result.IsSuccess);
                Assert.True(File.Exists(result.Value));
                Assert.True(File.Exists(result.Value + ".sha256"), $"Sidecar mancante: {result.Value}.sha256");
                Assert.Equal(ExportLocations.OutputDirectory(), Path.GetDirectoryName(result.Value));
            }
            finally
            {
                if (result.IsSuccess && File.Exists(result.Value))
                    File.Delete(result.Value);
                if (result.IsSuccess && File.Exists(result.Value + ".sha256"))
                    File.Delete(result.Value + ".sha256");
            }
        }

        [Fact]
        public async Task Sidecar_Contains_Hash_Of_File()
        {
            var exporter = new EntriesExporter(new FakeFormatter());

            var result = await exporter.SaveEntriesDataAsync(new List<Entry>(), EntryType.ShellBags);

            try
            {
                var sidecar = File.ReadAllText(result.Value + ".sha256");
                var hex = sidecar.Split(' ')[0].Trim();
                var expected = Activities_Inspector.Utils.IntegrityHasher.ComputeHex(await Activities_Inspector.Utils.FileRetry.ReadAllBytesAsync(result.Value));
                Assert.Equal(expected, hex);
            }
            finally
            {
                if (File.Exists(result.Value)) File.Delete(result.Value);
                if (File.Exists(result.Value + ".sha256")) File.Delete(result.Value + ".sha256");
            }
        }

        [Fact]
        public async Task Concurrent_Exports_Of_The_Same_Type_Get_Distinct_Files()
        {
            // Regressione dell'instabilita': il nome contiene l'ora al secondo e veniva scelto con "controlla se
            // esiste, poi crea": esportazioni dello stesso tipo nello stesso secondo collidevano (file in uso o
            // sovrascritto) e i test paralleli fallivano a intermittenza
            var exporter = new EntriesExporter(new FakeFormatter());
            var paths = new List<string>();

            try
            {
                var results = await Task.WhenAll(Enumerable.Range(0, 24)
                    .Select(_ => Task.Run(() => exporter.SaveEntriesDataAsync(new List<Entry>(), EntryType.ShellBags))));

                foreach (var r in results)
                {
                    Assert.True(r.IsSuccess, r.IsFailure ? r.Error : null);
                    paths.Add(r.Value);
                }

                Assert.Equal(24, paths.Distinct(StringComparer.OrdinalIgnoreCase).Count());
                Assert.All(paths, p =>
                {
                    Assert.True(File.Exists(p));
                    Assert.True(File.Exists(p + ".sha256"));
                });
            }
            finally
            {
                foreach (var p in paths)
                {
                    if (File.Exists(p)) File.Delete(p);
                    if (File.Exists(p + ".sha256")) File.Delete(p + ".sha256");
                }
            }
        }

        [Fact]
        public async Task File_Name_Uses_Two_Digit_Day_And_Month()
        {
            var exporter = new EntriesExporter(new FakeFormatter());
            var result = await exporter.SaveEntriesDataAsync(new List<Entry>(), EntryType.Usb);

            try
            {
                Assert.Matches(@"_\d{2}-\d{2}-\d{4}_\d{2}-\d{2}-\d{2}(_\d+)?\.csv$", Path.GetFileName(result.Value));
            }
            finally
            {
                if (result.IsSuccess && File.Exists(result.Value)) File.Delete(result.Value);
                if (result.IsSuccess && File.Exists(result.Value + ".sha256")) File.Delete(result.Value + ".sha256");
            }
        }

        private sealed class FakeFormatter : IEntryFormatter
        {
            public string AsCsv(Entry entry) => "row";
        }
    }
}
