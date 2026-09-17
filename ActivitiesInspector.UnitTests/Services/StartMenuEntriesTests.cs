using Activities_Inspector.Services;
using System;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;

namespace ActivitiesInspector.UnitTests.Services
{
    public class StartMenuEntriesTests
    {
        private static string WriteLnkPointingTo(string targetPath, string lnkPath)
        {
            var pathBytes = Encoding.ASCII.GetBytes(targetPath + "\0");
            int size = 28 + pathBytes.Length;
            var raw = new byte[76 + 4 + size];

            raw[0] = 0x4C;
            Buffer.BlockCopy(BitConverter.GetBytes(0x00000002), 0, raw, 20, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(size), 0, raw, 76, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(28), 0, raw, 80, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(1), 0, raw, 84, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(28), 0, raw, 92, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(28), 0, raw, 100, 4);
            Buffer.BlockCopy(pathBytes, 0, raw, 104, pathBytes.Length);

            File.WriteAllBytes(lnkPath, raw);
            return lnkPath;
        }

        private static string TempPath(string extension)
        {
            return Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + extension);
        }

        [Fact]
        public void Resolves_Local_Target()
        {
            string target = TempPath(".txt");
            string lnk = TempPath(".lnk");

            try
            {
                File.WriteAllText(target, "x");
                WriteLnkPointingTo(target, lnk);

                var entry = InstallEntriesBuilder.BuildStartMenuEntry(lnk);

                Assert.NotNull(entry);
                Assert.Equal(target, entry.FullPath);
                Assert.Equal(Path.GetFileNameWithoutExtension(target), entry.FileName);
                Assert.Equal(lnk, entry.DataSource);
            }
            finally
            {
                File.Delete(target);
                File.Delete(lnk);
            }
        }

        [Fact]
        public void Missing_Target_Returns_Null()
        {
            string lnk = TempPath(".lnk");

            try
            {
                WriteLnkPointingTo(Path.Combine(Path.GetTempPath(), "nope-mai-esistito-12345.exe"), lnk);

                Assert.Null(InstallEntriesBuilder.BuildStartMenuEntry(lnk));
            }
            finally
            {
                File.Delete(lnk);
            }
        }

        [Fact]
        public void NonLnk_Returns_Null()
        {
            string lnk = TempPath(".lnk");

            try
            {
                File.WriteAllBytes(lnk, new byte[] { 0x00, 0x01, 0x02 });

                Assert.Null(InstallEntriesBuilder.BuildStartMenuEntry(lnk));
            }
            finally
            {
                File.Delete(lnk);
            }
        }

        [Fact]
        public void EnumerateLnkFiles_Finds_Nested()
        {
            string root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            string sub = Path.Combine(root, "sub");

            try
            {
                Directory.CreateDirectory(sub);
                File.WriteAllText(Path.Combine(root, "a.lnk"), "x");
                File.WriteAllText(Path.Combine(sub, "b.lnk"), "x");
                File.WriteAllText(Path.Combine(sub, "c.txt"), "x");

                var found = InstallEntriesBuilder.EnumerateLnkFiles(root)
                    .Select(Path.GetFileName)
                    .OrderBy(n => n)
                    .ToList();

                Assert.Equal(new[] { "a.lnk", "b.lnk" }, found);
            }
            finally
            {
                Directory.Delete(root, true);
            }
        }
    }
}
