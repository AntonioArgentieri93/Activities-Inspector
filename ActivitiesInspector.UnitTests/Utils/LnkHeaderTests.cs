using Activities_Inspector.Utils;
using System;
using Xunit;

namespace ActivitiesInspector.UnitTests.Utils
{
    public class LnkHeaderTests
    {
        private static byte[] BuildHeader(int dataFlags, int fileAttributes, uint fileSize, int iconIndex, int showWindow)
        {
            var raw = new byte[76];

            Buffer.BlockCopy(BitConverter.GetBytes(76), 0, raw, 0, 4);

            var signature = new Guid("{00021401-0000-0000-c000-000000000046}");
            Buffer.BlockCopy(signature.ToByteArray(), 0, raw, 4, 16);

            Buffer.BlockCopy(BitConverter.GetBytes(dataFlags), 0, raw, 20, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(fileAttributes), 0, raw, 24, 4);

            // FILETIME a zero (28/36/44) => 01/01/1601 UTC.

            Buffer.BlockCopy(BitConverter.GetBytes(fileSize), 0, raw, 52, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(iconIndex), 0, raw, 56, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(showWindow), 0, raw, 60, 4);

            // HotKey (64-65) e Reserved (66/68/72) a zero.
            return raw;
        }

        [Fact]
        public void Parses_Signature_Flags_Attributes_Size()
        {
            var header = new LnkHeader(BuildHeader(0x00000081, 0x00000020, 123456u, 7, 1));

            Assert.Equal(new Guid("{00021401-0000-0000-c000-000000000046}"), header.Signature);
            Assert.True(header.DataFlags.HasFlag(LnkHeader.DataFlag.HasTargetIdList));
            Assert.True(header.DataFlags.HasFlag(LnkHeader.DataFlag.IsUnicode));
            Assert.Equal(LnkHeader.FileAttribute.FileAttributeArchive, header.FileAttributes);
            Assert.Equal(123456u, header.FileSize);
            Assert.Equal(7, header.IconIndex);
            Assert.Equal(LnkHeader.ShowWindowOption.SwNormal, header.ShowWindow);
            Assert.Equal(string.Empty, header.HotKey);
        }

        [Fact]
        public void Zero_FileTime_Maps_To_1601()
        {
            var header = new LnkHeader(BuildHeader(0, 0, 0u, 0, 0));

            Assert.Equal(new DateTimeOffset(1601, 1, 1, 0, 0, 0, TimeSpan.Zero), header.TargetCreationDate);
        }

        [Fact]
        public void HotKey_Control_A()
        {
            var raw = BuildHeader(0, 0, 0u, 0, 0);
            raw[64] = 0x41;
            raw[65] = 0x02;

            var header = new LnkHeader(raw);

            Assert.Equal("CONTROL+A", header.HotKey);
        }
    }
}
