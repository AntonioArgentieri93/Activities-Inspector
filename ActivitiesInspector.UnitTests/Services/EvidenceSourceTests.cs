using Activities_Inspector.Services.Evidence;
using System;
using System.IO;
using System.Linq;
using Xunit;

namespace ActivitiesInspector.UnitTests.Services
{
    public class EvidenceSourceTests : IDisposable
    {
        private readonly string _root;

        public EvidenceSourceTests()
        {
            _root = Path.Combine(Path.GetTempPath(), "evsrc_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(_root, "Windows", "Prefetch"));
            Directory.CreateDirectory(Path.Combine(_root, "Windows", "System32", "config"));
            Directory.CreateDirectory(Path.Combine(_root, "Users", "Alice", "AppData", "Roaming", "Microsoft", "Windows", "Recent"));
            Directory.CreateDirectory(Path.Combine(_root, "ProgramData", "Microsoft", "Windows", "Start Menu", "Programs"));
            File.WriteAllText(Path.Combine(_root, "Windows", "Prefetch", "A.pf"), "x");
            File.WriteAllText(Path.Combine(_root, "Windows", "System32", "config", "SYSTEM"), "x");
            File.WriteAllText(Path.Combine(_root, "Users", "Alice", "NTUSER.DAT"), "x");
        }

        public void Dispose()
        {
            try { Directory.Delete(_root, true); } catch { }
        }

        [Fact]
        public void Offline_Maps_Windows_Paths_Under_Root()
        {
            var source = new OfflineEvidenceSource(_root);

            Assert.False(source.IsLive);
            Assert.Equal(Path.Combine(_root, "Windows", "Prefetch"), source.MapPath(@"C:\Windows\Prefetch"));
            Assert.Equal(Path.Combine(_root, "Windows", "Prefetch"), source.MapPath(@"D:\Windows\Prefetch"));
        }

        [Fact]
        public void Offline_Enumerates_Files_Users_And_Hives()
        {
            var source = new OfflineEvidenceSource(_root);

            Assert.Single(source.EnumerateFiles(@"C:\Windows\Prefetch", "*.pf"));
            Assert.Single(source.GetUserProfileDirs());
            Assert.Single(source.GetRecentDirectories());
            Assert.Contains(source.GetStartMenuDirectories(),
                d => d.EndsWith("Programs"));
            Assert.True(File.Exists(source.GetSystemHivePath()));
            Assert.Single(source.GetUserHivePaths("NTUSER.DAT"));
            Assert.Empty(source.GetUserHivePaths("Missing.dat"));
        }

        [Fact]
        public void Live_Maps_Identity()
        {
            var source = new LiveEvidenceSource();

            Assert.True(source.IsLive);
            Assert.Equal(@"C:\Windows\Prefetch", source.MapPath(@"C:\Windows\Prefetch"));
            Assert.DoesNotContain(null, source.GetRecentDirectories().ToList());
        }

        [Fact]
        public void Provider_Switches_And_Validates()
        {
            var provider = new EvidenceSourceProvider();

            Assert.True(provider.Current.IsLive);

            provider.UseImage(_root);
            Assert.False(provider.Current.IsLive);
            Assert.Contains(_root, provider.Current.DisplayName);

            Assert.Throws<InvalidOperationException>(() => provider.UseImage(Path.Combine(_root, "Windows")));
            Assert.Throws<InvalidOperationException>(() => provider.UseImage(Path.Combine(Path.GetTempPath(), "inesistente_xyz")));

            provider.UseLive();
            Assert.True(provider.Current.IsLive);
        }
    }
}
