using Activities_Inspector.ExtraData;
using Activities_Inspector.ExtraData.ExtraData;
using Activities_Inspector.Utils;
using System;
using Xunit;

namespace ActivitiesInspector.UnitTests.Utils
{
    public class ExtraDataBlockFactoryTests
    {
        [Fact]
        public void Too_Short_Block_Yields_DamagedDataBlock()
        {
            var result = ExtraDataBlockFactory.Create(new byte[] { 0x01, 0x02, 0x03 });

            Assert.IsType<DamagedDataBlock>(result);
        }

        [Fact]
        public void Unknown_Signature_Yields_DamagedDataBlock()
        {
            var raw = new byte[12];
            Buffer.BlockCopy(BitConverter.GetBytes(unchecked((int)0xDEADBEEF)), 0, raw, 4, 4);

            var result = ExtraDataBlockFactory.Create(raw);

            Assert.IsType<DamagedDataBlock>(result);
        }
    }
}
