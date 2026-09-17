using Activities_Inspector.Services;
using Activities_Inspector.Utils;
using Activities_Inspector.Versions;
using System;
using System.IO;
using Xunit;

namespace ActivitiesInspector.UnitTests.Services
{
    /// <summary>
    /// Fixture sintetiche per ogni versione di prefetch supportata
    /// (XP/2003=17, Vista/7=23, 8.x=26, 10/11=30). Verificano dispatch
    /// e parsing senza bisogno di file reali dei vari sistemi operativi.
    /// </summary>
    public class PrefetchFileParserServiceTests
    {
        private static readonly DateTimeOffset FirstRun =
            new DateTimeOffset(2024, 1, 15, 0, 0, 0, TimeSpan.Zero);

        private static readonly DateTimeOffset SecondRun =
            new DateTimeOffset(2024, 2, 20, 0, 0, 0, TimeSpan.Zero);

        private static byte[] BuildFixture(int version)
        {
            var raw = new byte[512];

            Buffer.BlockCopy(BitConverter.GetBytes(version), 0, raw, 0, 4);
            Buffer.BlockCopy(System.Text.Encoding.ASCII.GetBytes("SCCA"), 0, raw, 4, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(512), 0, raw, 12, 4);

            var nameBytes = System.Text.Encoding.Unicode.GetBytes("NOTEPAD.EXE\0");
            Buffer.BlockCopy(nameBytes, 0, raw, 16, nameBytes.Length);

            // Tabella offset con conteggi a zero: nessun blocco secondario.
            // Gli offset puntano dentro il buffer per le BlockCopy a size 0.
            foreach (var fieldOffset in new[] { 0, 8, 16, 24 })
            {
                Buffer.BlockCopy(BitConverter.GetBytes(400), 0, raw, 84 + fieldOffset, 4);
            }

            return raw;
        }

        private static void WriteFileTime(byte[] raw, int offset, DateTimeOffset value)
        {
            var utc = new DateTime(value.Ticks, DateTimeKind.Utc);
            Buffer.BlockCopy(BitConverter.GetBytes(utc.ToFileTime()), 0, raw, offset, 8);
        }

        private static IPrefetch ParseFixture(byte[] raw)
        {
            string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".pf");

            try
            {
                File.WriteAllBytes(path, raw);
                return new PrefetchFileParserService().Open(path);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public void Version17_Xp_Dispatch_And_Parse()
        {
            var raw = BuildFixture(17);
            WriteFileTime(raw, 84 + 36, FirstRun);
            Buffer.BlockCopy(BitConverter.GetBytes(3), 0, raw, 84 + 60, 4);

            var pf = Assert.IsType<Version17>(ParseFixture(raw));

            Assert.Equal("NOTEPAD.EXE", pf.Header.ExecutableFilename);
            Assert.False(pf.ParsingError);
            Assert.Equal(1, pf.LastRunTimes.Count);
            Assert.Equal(FirstRun, pf.LastRunTimes[0]);
            Assert.Equal(3, pf.RunCount);
        }

        [Fact]
        public void Version23_Vista7_Dispatch_And_Parse()
        {
            var raw = BuildFixture(23);
            WriteFileTime(raw, 84 + 44, FirstRun);
            Buffer.BlockCopy(BitConverter.GetBytes(7), 0, raw, 84 + 68, 4);

            var pf = Assert.IsType<Version23>(ParseFixture(raw));

            Assert.Equal("NOTEPAD.EXE", pf.Header.ExecutableFilename);
            Assert.False(pf.ParsingError);
            Assert.Equal(1, pf.LastRunTimes.Count);
            Assert.Equal(FirstRun, pf.LastRunTimes[0]);
            Assert.Equal(7, pf.RunCount);
        }

        [Fact]
        public void Version26_Win8_Dispatch_And_Parse()
        {
            var raw = BuildFixture(26);
            WriteFileTime(raw, 84 + 44, FirstRun);
            WriteFileTime(raw, 84 + 52, SecondRun);
            Buffer.BlockCopy(BitConverter.GetBytes(9), 0, raw, 84 + 124, 4);

            var pf = Assert.IsType<Version26>(ParseFixture(raw));

            Assert.Equal("NOTEPAD.EXE", pf.Header.ExecutableFilename);
            Assert.False(pf.ParsingError);
            Assert.Equal(2, pf.LastRunTimes.Count);
            Assert.Equal(FirstRun, pf.LastRunTimes[0]);
            Assert.Equal(SecondRun, pf.LastRunTimes[1]);
            Assert.Equal(9, pf.RunCount);
        }

        [Fact]
        public void Version30_Win10_Dispatch_And_Parse()
        {
            var raw = BuildFixture(30);
            WriteFileTime(raw, 84 + 44, FirstRun);
            WriteFileTime(raw, 84 + 52, SecondRun);
            Buffer.BlockCopy(BitConverter.GetBytes(9), 0, raw, 84 + 124, 4);

            var pf = Assert.IsType<Version30>(ParseFixture(raw));

            Assert.Equal("NOTEPAD.EXE", pf.Header.ExecutableFilename);
            Assert.False(pf.ParsingError);
            Assert.Equal(2, pf.LastRunTimes.Count);
            Assert.Equal(FirstRun, pf.LastRunTimes[0]);
            Assert.Equal(SecondRun, pf.LastRunTimes[1]);
            Assert.Equal(9, pf.RunCount);
        }
    }
}
