using Activities_Inspector.ShellBags.ShellBags;
using Activities_Inspector.Utils;
using System;
using Xunit;

namespace ActivitiesInspector.UnitTests.Utils
{
    public class LnkFileTests
    {
        [Fact]
        public void Malformed_Item_Is_Skipped_Rest_Parsed()
        {
            // Header 76 byte con solo HasTargetIdList + ID list con
            // un item valido (tipo 0x01) e uno con ID sconosciuto (0xFF).
            var raw = new byte[103];

            Buffer.BlockCopy(BitConverter.GetBytes(0x00000001), 0, raw, 20, 4);
            Buffer.BlockCopy(BitConverter.GetBytes((short)25), 0, raw, 76, 2);

            // La size include i 2 byte di header: 2 + 16 di contenuto.
            raw[78] = 18;
            raw[79] = 0;
            raw[80] = 0x01;

            raw[96] = 3;
            raw[97] = 0;
            raw[98] = 0xFF;

            var lnk = new LnkFile(raw, "test.lnk");

            Assert.Equal(1, lnk.TargetIDs.Count);
            Assert.IsType<ShellBag0X01>(lnk.TargetIDs[0]);
            Assert.Equal(1, lnk.SkippedShellItems);
        }

        [Fact]
        public void Truncated_LinkInfo_Does_Not_Throw()
        {
            var raw = new byte[80];

            raw[0] = 0x4C;
            Buffer.BlockCopy(BitConverter.GetBytes(0x00000002), 0, raw, 20, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(0xFFFFFF), 0, raw, 76, 4);

            var lnk = new LnkFile(raw, "test.lnk");

            Assert.Empty(lnk.TargetIDs);
            Assert.Null(lnk.LocalPath);
        }

        [Fact]
        public void Clean_File_Has_Zero_Skipped()
        {
            var raw = new byte[96];

            Buffer.BlockCopy(BitConverter.GetBytes(0x00000001), 0, raw, 20, 4);
            Buffer.BlockCopy(BitConverter.GetBytes((short)18), 0, raw, 76, 2);

            raw[78] = 18;
            raw[79] = 0;
            raw[80] = 0x01;

            var lnk = new LnkFile(raw, "test.lnk");

            Assert.Equal(1, lnk.TargetIDs.Count);
            Assert.Equal(0, lnk.SkippedShellItems);
        }
    }
}
