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

        // Ordine reale del formato Prefetch: dal più recente al più vecchio
        private static readonly List<DateTimeOffset> NewestFirstRuns = new List<DateTimeOffset>
        {
            new DateTimeOffset(2024, 2, 20, 12, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2024, 1, 15, 12, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2024, 1, 10, 12, 0, 0, TimeSpan.Zero)
        };

        [Fact]
        public void Last_Run_Is_Most_Recent_Timestamp()
        {
            // Regressione: Last()/First() invertivano ultima e prima esecuzione (es. ACROBAT.EXE)
            var entry = PrefetchFileInfoBuilderService.BuildEntry(
                new FakePrefetch(NewestFirstRuns, 3), "APP.EXE", "APP.EXE-AB12.pf", ".EXE");

            Assert.NotNull(entry);
            Assert.Equal("APP.EXE", entry.ExecutableFileName);
            Assert.Equal(new DateTime(2024, 2, 20), entry.LastRunTime.Date);
            Assert.Equal(new DateTime(2024, 1, 10), entry.FirstRunTime?.Date);
            Assert.Equal(3, entry.RunCount);
        }

        [Fact]
        public void First_Run_Is_Not_Available_When_Not_All_Runs_Are_Recorded()
        {
            // 42 esecuzioni ma solo 3 timestamp: la più vecchia registrata non è la prima esecuzione
            var entry = PrefetchFileInfoBuilderService.BuildEntry(
                new FakePrefetch(NewestFirstRuns, 42), "APP.EXE", "APP.EXE-AB12.pf", ".EXE");

            Assert.Equal(new DateTime(2024, 2, 20), entry.LastRunTime.Date);
            Assert.Null(entry.FirstRunTime);
        }

        [Fact]
        public void Single_Run_Is_Both_First_And_Last()
        {
            var run = new List<DateTimeOffset> { new DateTimeOffset(2024, 3, 1, 9, 0, 0, TimeSpan.Zero) };
            var entry = PrefetchFileInfoBuilderService.BuildEntry(new FakePrefetch(run, 1), "APP.EXE", "x.pf", ".EXE");

            Assert.Equal(entry.LastRunTime, entry.FirstRunTime);
        }

        [Theory]
        [InlineData("MICROSOFT.AAD.BROKERPLUGIN.EX", "MICROSOFT.AAD.BROKERPLUGIN.EXE")]
        [InlineData("OLKPUSHNOTIFICATIONBACKGROUND", "OLKPUSHNOTIFICATIONBACKGROUNDTASK.EXE")]
        [InlineData("SERVICEHUB.THREADEDWAITDIALOG", "SERVICEHUB.THREADEDWAITDIALOG.EXE")]  // .EXE preferito alla .DLL
        public void Truncated_Header_Name_Is_Resolved_From_Filenames(string header, string expected)
        {
            var files = new List<string>
            {
                @"\VOLUME{01d}\WINDOWS\SYSTEM32\NTDLL.DLL",
                @"\VOLUME{01d}\PROGRAM FILES\X\SERVICEHUB.THREADEDWAITDIALOG.DLL",
                @"\VOLUME{01d}\PROGRAM FILES\X\SERVICEHUB.THREADEDWAITDIALOG.EXE",
                @"\VOLUME{01d}\WINDOWS\SYSTEMAPPS\MICROSOFT.AAD.BROKERPLUGIN.EXE",
                @"\VOLUME{01d}\PROGRAM FILES\OUTLOOK\OLKPUSHNOTIFICATIONBACKGROUNDTASK.EXE",
            };

            Assert.Equal(expected, PrefetchFileInfoBuilderService.ResolveExecutableName(header, files));
        }

        [Theory]
        [InlineData("ACTIVITIES INSPECTOR.EXE")]   // non troncato: l'header vince (NirSoft qui mostra la .DLL)
        [InlineData("MICROSOFTEDGE_X64_154.0.4258.")]  // troncato ma senza corrispondenze: resta com'è
        public void Header_Name_Is_Kept_When_Not_Truncated_Or_Not_Found(string header)
        {
            var files = new List<string> { @"\VOLUME{01d}\X\ACTIVITIES INSPECTOR.DLL", @"\VOLUME{01d}\X\ACTIVITIES INSPECTOR.EXE.CONFIG" };
            Assert.Equal(header, PrefetchFileInfoBuilderService.ResolveExecutableName(header, files));
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
