using Activities_Inspector.Utils;
using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace ActivitiesInspector.UnitTests.Utils
{
    public class FileRetryTests
    {
        [Fact]
        public async Task Reads_A_Normal_File()
        {
            var path = Path.GetTempFileName();
            try
            {
                File.WriteAllBytes(path, new byte[] { 1, 2, 3 });

                Assert.Equal(new byte[] { 1, 2, 3 }, await FileRetry.ReadAllBytesAsync(path));
            }
            finally { File.Delete(path); }
        }

        [Fact]
        public async Task Succeeds_Once_A_Temporary_Lock_Is_Released()
        {
            // Simula l'antivirus che tiene il file aperto per un istante dopo la scrittura
            var path = Path.GetTempFileName();
            try
            {
                File.WriteAllBytes(path, new byte[] { 9 });
                var locker = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
                var release = Task.Run(async () => { await Task.Delay(150); locker.Dispose(); });

                var bytes = await FileRetry.ReadAllBytesAsync(path);
                await release;

                Assert.Equal(new byte[] { 9 }, bytes);
            }
            finally { File.Delete(path); }
        }

        [Fact]
        public async Task A_Missing_File_Fails_Immediately_Without_Waiting()
        {
            var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            var started = DateTime.UtcNow;

            await Assert.ThrowsAsync<FileNotFoundException>(() => FileRetry.ReadAllBytesAsync(path));

            Assert.True(DateTime.UtcNow - started < TimeSpan.FromSeconds(1));
        }

        [Fact]
        public async Task A_Permanent_Lock_Fails_After_The_Attempts()
        {
            var path = Path.GetTempFileName();
            using (new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            {
                await Assert.ThrowsAsync<IOException>(() => FileRetry.ReadAllBytesAsync(path, attempts: 3));
            }
            File.Delete(path);
        }
    }
}
