using Activities_Inspector.Models;
using Activities_Inspector.Services;
using Activities_Inspector.Utils;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;

namespace ActivitiesInspector.UnitTests.Utils
{
    public class EntryWriterTests
    {
        [Fact]
        public void Footer_Note_Appended_As_Last_Line()
        {
            var path = Path.GetTempFileName();
            try
            {
                using (var writer = new EntryWriter(path, false, Encoding.UTF8, new FakeFormatter()))
                {
                    writer.WriteEntries(new List<Entry>(), EntryType.ShellBags, "NOTA");
                }

                var lines = File.ReadAllLines(path).Where(l => !string.IsNullOrEmpty(l)).ToArray();

                Assert.Equal(2, lines.Length);
                Assert.Equal("NOTA", lines[1]);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public void No_Footer_Note_Writes_Header_Only_For_Empty()
        {
            var path = Path.GetTempFileName();
            try
            {
                using (var writer = new EntryWriter(path, false, Encoding.UTF8, new FakeFormatter()))
                {
                    writer.WriteEntries(new List<Entry>(), EntryType.ShellBags);
                }

                var lines = File.ReadAllLines(path).Where(l => !string.IsNullOrEmpty(l)).ToArray();

                Assert.Single(lines);
            }
            finally
            {
                File.Delete(path);
            }
        }

        private sealed class FakeFormatter : IEntryFormatter
        {
            public string AsCsv(Entry entry) => "row";
        }
    }
}
