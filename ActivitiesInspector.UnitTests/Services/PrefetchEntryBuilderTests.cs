using Activities_Inspector.Models;
using Activities_Inspector.Services;
using Activities_Inspector.Utils;
using System;
using System.Collections.Generic;
using Xunit;

namespace ActivitiesInspector.UnitTests.Services
{
    public class PrefetchEntryBuilderTests
    {
        private sealed class FakePrefetch : IPrefetch
        {
            public FakePrefetch(List<DateTimeOffset> runs, int runCount)
            {
                LastRunTimes = runs;
                RunCount = runCount;
            }

            public byte[] RawBytes => Array.Empty<byte>();
            public string SourceFilename => "fake.pf";
            public DateTimeOffset SourceCreatedOn => DateTimeOffset.MinValue;
            public DateTimeOffset SourceModifiedOn => DateTimeOffset.MinValue;
            public DateTimeOffset SourceAccessedOn => DateTimeOffset.MinValue;
            public Header Header => new Header(new byte[84]);
            public int FileMetricsOffset => 0;
            public int FileMetricsCount => 0;
            public int TraceChainsOffset => 0;
            public int TraceChainsCount => 0;
            public int FilenameStringsOffset => 0;
            public int FilenameStringsSize => 0;
            public int VolumesInfoOffset => 0;
            public int VolumeCount => 0;
            public int VolumesInfoSize => 0;
            public int TotalDirectoryCount => 0;
            public List<DateTimeOffset> LastRunTimes { get; }
            public List<VolumeInfo> VolumeInformation => new List<VolumeInfo>();
            public int RunCount { get; }
            public bool ParsingError => false;
            public List<string> Filenames => new List<string>();
            public List<FileMetric> FileMetrics => new List<FileMetric>();
            public List<TraceChain> TraceChains => new List<TraceChain>();
        }

        [Fact]
        public void Maps_First_Last_And_RunCount()
        {
            var runs = new List<DateTimeOffset>
            {
                new DateTimeOffset(2024, 1, 10, 0, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2024, 1, 15, 0, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2024, 2, 20, 0, 0, 0, TimeSpan.Zero)
            };

            var entry = PrefetchFileInfoBuilderService.BuildEntry(
                new FakePrefetch(runs, 42), "APP.EXE", "APP.EXE-AB12.pf", ".EXE");

            Assert.NotNull(entry);
            Assert.Equal("APP.EXE", entry.ExecutableFileName);
            Assert.Equal(new DateTime(2024, 2, 20), entry.LastRunTime.Date);
            Assert.Equal(new DateTime(2024, 1, 10), entry.FirstRunTime.Date);
            Assert.Equal(42, entry.RunCount);
        }

        [Fact]
        public void Empty_Runs_Returns_Null()
        {
            var entry = PrefetchFileInfoBuilderService.BuildEntry(
                new FakePrefetch(new List<DateTimeOffset>(), 0), "APP.EXE", "x.pf", ".EXE");

            Assert.Null(entry);
        }
    }
}
