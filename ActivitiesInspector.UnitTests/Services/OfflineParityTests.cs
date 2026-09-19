using Activities_Inspector.Models;
using Activities_Inspector.Services;
using Activities_Inspector.Services.Evidence;
using Activities_Inspector.Utils;
using Activities_Inspector.Versions;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace ActivitiesInspector.UnitTests.Services
{
    public class OfflineParityTests : IDisposable
    {
        private static readonly DateTimeOffset FirstRun =
            new DateTimeOffset(2024, 1, 15, 0, 0, 0, TimeSpan.Zero);
        private static readonly DateTimeOffset SecondRun =
            new DateTimeOffset(2024, 2, 20, 0, 0, 0, TimeSpan.Zero);

        private readonly string _root;

        public OfflineParityTests()
        {
            _root = Path.Combine(Path.GetTempPath(), "offpar_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(_root, "Windows", "Prefetch"));
            Directory.CreateDirectory(Path.Combine(_root, "Users", "Bob", "AppData", "Roaming", "Microsoft", "Windows", "Recent"));
        }

        public void Dispose()
        {
            try { Directory.Delete(_root, true); } catch { }
        }

        private static EvidenceSourceProvider OfflineProvider(string root)
        {
            var provider = new EvidenceSourceProvider();
            provider.UseImage(root);
            return provider;
        }

        private static byte[] BuildV30Fixture()
        {
            var raw = new byte[512];
            Buffer.BlockCopy(BitConverter.GetBytes(30), 0, raw, 0, 4);
            Buffer.BlockCopy(System.Text.Encoding.ASCII.GetBytes("SCCA"), 0, raw, 4, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(512), 0, raw, 12, 4);
            var nameBytes = System.Text.Encoding.Unicode.GetBytes("NOTEPAD.EXE\0");
            Buffer.BlockCopy(nameBytes, 0, raw, 16, nameBytes.Length);
            foreach (var fieldOffset in new[] { 0, 8, 16, 24 })
                Buffer.BlockCopy(BitConverter.GetBytes(400), 0, raw, 84 + fieldOffset, 4);
            WriteFileTime(raw, 84 + 44, FirstRun);
            WriteFileTime(raw, 84 + 52, SecondRun);
            Buffer.BlockCopy(BitConverter.GetBytes(9), 0, raw, 84 + 124, 4);
            return raw;
        }

        private static void WriteFileTime(byte[] raw, int offset, DateTimeOffset value)
        {
            var utc = new DateTime(value.Ticks, DateTimeKind.Utc);
            Buffer.BlockCopy(BitConverter.GetBytes(utc.ToFileTime()), 0, raw, offset, 8);
        }

        [Fact]
        public async Task Prefetch_Offline_Equals_Direct_Parse_Of_Same_Bytes()
        {
            var fixture = BuildV30Fixture();
            var imageFile = Path.Combine(_root, "Windows", "Prefetch", "NOTEPAD-ABC.pf");
            var directFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".pf");
            File.WriteAllBytes(imageFile, fixture);
            File.WriteAllBytes(directFile, fixture);

            try
            {
                var parser = new PrefetchFileParserService();
                var service = new PrefetchFileInfoBuilderService(parser, OfflineProvider(_root));

                var result = await service.GetPrefetchFileInfosAsync();

                Assert.True(result.IsSuccess);
                var entry = Assert.Single(result.Value);
                Assert.Equal("NOTEPAD.EXE", entry.ExecutableFileName);
                Assert.Equal(9, entry.RunCount);

                var direct = PrefetchFileInfoBuilderService.BuildEntry(
                    parser.Open(directFile), "NOTEPAD.EXE", "NOTEPAD-ABC.pf", ".EXE");
                Assert.Equal(direct.ExecutableFileName, entry.ExecutableFileName);
                Assert.Equal(direct.RunCount, entry.RunCount);
                Assert.Equal(direct.FirstRunTime, entry.FirstRunTime);
                Assert.Equal(direct.LastRunTime, entry.LastRunTime);

                var manifest = Assert.Single(service.LastIntegrityManifest);
                Assert.Equal(IntegrityStatus.Acquired, manifest.Status);
                Assert.Equal(512, manifest.SizeBytes);
            }
            finally
            {
                File.Delete(directFile);
            }
        }

        [Fact]
        public async Task Prefetch_Offline_Missing_Dir_Fails_Explicitly()
        {
            var emptyRoot = Path.Combine(Path.GetTempPath(), "offempty_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(emptyRoot, "Windows"));

            try
            {
                var service = new PrefetchFileInfoBuilderService(
                    new PrefetchFileParserService(), OfflineProvider(emptyRoot));

                var result = await service.GetPrefetchFileInfosAsync();

                Assert.False(result.IsSuccess);
            }
            finally
            {
                Directory.Delete(emptyRoot, true);
            }
        }

        [Fact]
        public async Task Recent_Offline_Records_Manifest_For_Every_File()
        {
            File.WriteAllBytes(Path.Combine(_root, "Users", "Bob", "AppData", "Roaming", "Microsoft", "Windows", "Recent", "a.lnk"), new byte[] { 0x4C });
            File.WriteAllBytes(Path.Combine(_root, "Users", "Bob", "AppData", "Roaming", "Microsoft", "Windows", "Recent", "b.lnk"), new byte[] { 0x00 });

            var service = new RecentFilesService(OfflineProvider(_root));

            var result = await service.GetRecentFilesAsync();

            Assert.True(result.IsSuccess);
            Assert.Equal(2, service.LastIntegrityManifest.Count);
            Assert.All(service.LastIntegrityManifest, r => Assert.Equal(IntegrityStatus.Acquired, r.Status));
        }

        [Fact]
        public async Task Recent_Offline_No_Profiles_Fails_Explicitly()
        {
            var emptyRoot = Path.Combine(Path.GetTempPath(), "offempty_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(emptyRoot, "Windows"));

            try
            {
                var service = new RecentFilesService(OfflineProvider(emptyRoot));

                var result = await service.GetRecentFilesAsync();

                Assert.False(result.IsSuccess);
            }
            finally
            {
                Directory.Delete(emptyRoot, true);
            }
        }

        [Fact]
        public async Task EventLog_Services_Record_Manifest_Row_On_Missing_Evtx()
        {
            var provider = OfflineProvider(_root);

            var sessions = new LoggedInfoService(provider);
            var intervals = new UsageLogTimeService(provider);
            var timeChanged = new SystemTimeChangedService(provider);

            await sessions.GetSessionsAsync();
            await intervals.GetSystemEventsAsync();
            await timeChanged.GetSystemTimeChangedEntriesAsync();

            var sRow = Assert.Single(sessions.LastIntegrityManifest);
            Assert.Equal(IntegrityStatus.NotAcquirable, sRow.Status);
            Assert.Equal(EntryType.Sessions, sRow.Feature);

            var uRow = Assert.Single(intervals.LastIntegrityManifest);
            Assert.Equal(IntegrityStatus.NotAcquirable, uRow.Status);
            Assert.Equal(EntryType.TimeIntervals, uRow.Feature);

            var tRow = Assert.Single(timeChanged.LastIntegrityManifest);
            Assert.Equal(IntegrityStatus.NotAcquirable, tRow.Status);
            Assert.Equal(EntryType.SystemTimeChanged, tRow.Feature);
        }

        [Fact]
        public async Task UsageLog_Live_Records_LiveSource_Manifest_Row()
        {
            var service = new UsageLogTimeService(new EvidenceSourceProvider());

            var result = await service.GetSystemEventsAsync();

            Assert.True(result.IsSuccess);
            var row = Assert.Single(service.LastIntegrityManifest);
            Assert.Equal(IntegrityStatus.LiveSource, row.Status);
            Assert.Equal(EntryType.TimeIntervals, row.Feature);
        }

        [Fact]
        public async Task EventLog_Services_Fail_Explicitly_On_Missing_Evtx()
        {
            var provider = OfflineProvider(_root);

            var sessions = await new LoggedInfoService(provider).GetSessionsAsync();
            var intervals = await new UsageLogTimeService(provider).GetSystemEventsAsync();
            var timeChanged = await new SystemTimeChangedService(provider).GetSystemTimeChangedEntriesAsync();

            Assert.False(sessions.IsSuccess);
            Assert.False(intervals.IsSuccess);
            Assert.False(timeChanged.IsSuccess);
        }

        [Fact]
        public async Task Usb_Offline_Missing_Hive_Fails_Explicitly()
        {
            var service = new UsbTrackingService(OfflineProvider(_root));

            var result = await service.BuildUsbEntriesAsync(false, default);

            Assert.False(result.IsSuccess);
        }

        [Fact]
        public async Task Install_Offline_Empty_Image_Fails_Explicitly()
        {
            var service = new InstallEntriesBuilder(OfflineProvider(_root));

            var result = await service.GetInstallEntriesAsync();

            Assert.False(result.IsSuccess);
        }

        [Fact]
        public void Install_Hive_Walker_Matches_Live_Or_Degrades_Explicitly()
        {
            var ntuser = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "NTUSER.DAT");

            var live = new System.Collections.Generic.List<(string Name, DateTime? Date)>();
            using (var liveKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall"))
            {
                if (liveKey != null)
                {
                    foreach (var sub in liveKey.GetSubKeyNames())
                    {
                        using var sk = liveKey.OpenSubKey(sub);
                        if (sk == null) continue;
                        var displayName = sk.GetValue("DisplayName")?.ToString();
                        if (!InstallEntriesBuilder.ShouldInclude(displayName,
                            sk.GetValue("SystemComponent")?.ToString(),
                            sk.GetValue("ParentKeyName")?.ToString(),
                            sk.GetValue("ReleaseType")?.ToString())) continue;
                        live.Add((displayName,
                            DateBuilder.BuildDateTimeFromString(sk.GetValue("InstallDate")?.ToString())));
                    }
                }
            }

            var manifest = new System.Collections.Generic.List<IntegrityRecord>();
            var offline = InstallEntriesBuilder.GetUninstallFromHiveFile(ntuser,
                new[] { @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall" },
                manifest, default)
                .Select(e => (e.FileName, e.InstallDate))
                .OrderBy(x => x.FileName).ThenBy(x => x.InstallDate)
                .ToList();

            var row = Assert.Single(manifest);
            if (row.Status == IntegrityStatus.Acquired)
            {
                Assert.Equal(
                    live.OrderBy(x => x.Name).ThenBy(x => x.Date).ToList(),
                    offline);
            }
            else
            {
                Assert.Equal(IntegrityStatus.NotAcquirable, row.Status);
                Assert.Empty(offline);
            }
        }

        [Fact]
        public void OfflineRegistryReader_Garbage_File_Returns_Empty_Without_Throwing()
        {
            var garbage = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".dat");
            File.WriteAllBytes(garbage, new byte[] { 0x01, 0x02, 0x03 });

            try
            {
                var keys = new OfflineRegistryReader(null, garbage).GetRegistryKeys();

                Assert.Empty(keys);
            }
            finally
            {
                File.Delete(garbage);
            }
        }

        [Fact]
        public void ShellBagParser_Empty_Reader_Is_Complete_Empty()
        {
            var (items, truncated) = ShellBagParser.GetShellItems(new EmptyReader());

            Assert.Empty(items);
            Assert.False(truncated);
        }

        private sealed class EmptyReader : IRegistryReader
        {
            public System.Collections.Generic.List<RegistryKeyWrapper> GetRegistryKeys()
                => new System.Collections.Generic.List<RegistryKeyWrapper>();
        }
    }
}
