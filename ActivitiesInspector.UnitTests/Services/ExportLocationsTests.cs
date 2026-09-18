using Activities_Inspector.Models;
using Activities_Inspector.Services;
using System;
using System.Collections.Generic;
using System.IO;
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
                Assert.Equal(ExportLocations.OutputDirectory(), Path.GetDirectoryName(result.Value));
            }
            finally
            {
                if (result.IsSuccess && File.Exists(result.Value))
                    File.Delete(result.Value);
            }
        }

        private sealed class FakeFormatter : IEntryFormatter
        {
            public string AsCsv(Entry entry) => "row";
        }
    }
}
