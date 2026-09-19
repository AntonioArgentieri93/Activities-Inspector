using Activities_Inspector.Models;
using Activities_Inspector.Services;
using Activities_Inspector.Services.Evidence;
using Activities_Inspector.Utils;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace ActivitiesInspector.UnitTests.Services
{
    public class ValidationPilotTests
    {
        private static string FullImageRoot => Path.Combine(Path.GetTempPath(), "FullImage");
        private static string OfflineKitRoot => Path.Combine(Path.GetTempPath(), "OfflineKit");

        private static EvidenceSourceProvider Offline(string root)
        {
            var p = new EvidenceSourceProvider();
            if (!Directory.Exists(root) || !Directory.Exists(Path.Combine(root, "Windows")))
                return null;
            p.UseImage(root);
            return p;
        }

        [Fact]
        public async Task Prefetch_Pilot_Offline_vs_Reference_Minimal_Parser()
        {
            var provider = Offline(FullImageRoot) ?? Offline(OfflineKitRoot);
            if (provider == null) throw new Xunit.Sdk.SkipException("Immagine assente in %TEMP%\\FullImage|OfflineKit — pilota saltato");

            var service = new PrefetchFileInfoBuilderService(new PrefetchFileParserService(), provider);
            var result = await service.GetPrefetchFileInfosAsync();
            Assert.True(result.IsSuccess);

            var files = provider.Current.EnumerateFiles(@"C:\Windows\Prefetch", "*.pf").ToList();
            Assert.Equal(files.Count, service.LastIntegrityManifest.Count);
            Assert.All(service.LastIntegrityManifest, r => Assert.Equal(IntegrityStatus.Acquired, r.Status));

            // Reference minimal parser: reads signature + executable name at fixed offsets, independent of VersionXX classes
            foreach (var file in files.Take(20))
            {
                var raw = await File.ReadAllBytesAsync(file);
                var refName = ReferencePrefetchName(raw);
                var refValid = refName != null;
                var found = result.Value.FirstOrDefault(e => e.SourceFileName.Equals(Path.GetFileName(file), StringComparison.OrdinalIgnoreCase));
                if (refValid)
                {
                    Assert.NotNull(found);
                    Assert.Equal(refName, found.ExecutableFileName);
                }
                else
                {
                    Assert.Null(found);
                }
            }
        }

        private static string ReferencePrefetchName(byte[] raw)
        {
            if (raw.Length < 16) return null;
            var version = BitConverter.ToInt32(raw, 0);
            if (version != 17 && version != 23 && version != 26 && version != 30 && version != 31) return null;
            if (Encoding.ASCII.GetString(raw, 4, 4) != "SCCA") return null;
            try
            {
                var nameBytes = new byte[60];
                Buffer.BlockCopy(raw, 16, nameBytes, 0, 60);
                var name = Encoding.Unicode.GetString(nameBytes).TrimEnd('\0');
                return string.IsNullOrWhiteSpace(name) ? null : name;
            }
            catch { return null; }
        }

        [Fact]
        public async Task Install_Pilot_Offline_Filtering_Matches_Direct_Hive_Enumeration()
        {
            var provider = Offline(FullImageRoot) ?? Offline(OfflineKitRoot);
            if (provider == null) throw new Xunit.Sdk.SkipException("Immagine assente in %TEMP%\\FullImage|OfflineKit — pilota saltato");

            var service = new InstallEntriesBuilder(provider);
            var result = await service.GetInstallEntriesAsync();
            Assert.True(result.IsSuccess);

            // Direct enumeration via RegistryHive on same file, with same ShouldInclude logic applied manually
            var hivePath = provider.Current.GetSoftwareHivePath();
            if (!File.Exists(hivePath)) throw new Xunit.Sdk.SkipException("Hive SOFTWARE assente — pilota saltato");

            var direct = InstallEntriesBuilder.GetUninstallFromHiveFile(hivePath,
                new[] { @"WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall", @"Microsoft\Windows\CurrentVersion\Uninstall" },
                new System.Collections.Generic.List<IntegrityRecord>(), default);

            // Our service merges both branches into one list; direct here is combined as well
            var ourNames = result.Value.Select(e => e.FileName).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var directNames = direct.Select(e => e.FileName).ToHashSet(StringComparer.OrdinalIgnoreCase);

            var missing = directNames.Where(n => !ourNames.Contains(n)).Take(5).ToList();
            Assert.True(missing.Count == 0, "mancanti: " + string.Join(", ", missing));
        }
    }
}
