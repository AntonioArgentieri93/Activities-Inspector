using Activities_Inspector.Models;
using Activities_Inspector.Utils;
using System.IO;
using System.Text;
using Xunit;

namespace ActivitiesInspector.UnitTests.Utils
{
    public class IntegrityHasherTests
    {
        [Fact]
        public void Known_Vector_abc()
        {
            var path = WriteTemp("abc");

            try
            {
                var record = IntegrityHasher.HashFile(path, EntryType.Prefetch);

                Assert.Equal(IntegrityStatus.Acquired, record.Status);
                Assert.Equal("ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad", record.Sha256);
                Assert.Equal(3, record.SizeBytes);
                Assert.NotNull(record.AcquiredUtc);
                Assert.Equal(path, record.Path);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public void Known_Vector_Empty()
        {
            var path = WriteTemp(string.Empty);

            try
            {
                var record = IntegrityHasher.HashFile(path, EntryType.Recents);

                Assert.Equal(IntegrityStatus.Acquired, record.Status);
                Assert.Equal("e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855", record.Sha256);
                Assert.Equal(0, record.SizeBytes);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public void HashBytes_Matches_HashFile_On_Same_Content()
        {
            var path = WriteTemp("abc");

            try
            {
                var fromBytes = IntegrityHasher.HashBytes(Encoding.ASCII.GetBytes("abc"), path, EntryType.Prefetch);
                var fromFile = IntegrityHasher.HashFile(path, EntryType.Prefetch);

                Assert.Equal(fromFile.Sha256, fromBytes.Sha256);
                Assert.Equal(fromFile.SizeBytes, fromBytes.SizeBytes);
                Assert.Equal(IntegrityStatus.Acquired, fromBytes.Status);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public void Missing_File_Yields_Explicit_Row_Without_Throwing()
        {
            var record = IntegrityHasher.HashFile(
                Path.Combine(Path.GetTempPath(), "inesistente_activities_inspector.bin"),
                EntryType.Usb);

            Assert.Equal(IntegrityStatus.NotAcquirable, record.Status);
            Assert.Null(record.Sha256);
            Assert.Null(record.SizeBytes);
            Assert.NotNull(record.Detail);
        }

        private static string WriteTemp(string content)
        {
            var path = Path.GetTempFileName();
            File.WriteAllText(path, content, Encoding.ASCII);
            return path;
        }
    }
}
