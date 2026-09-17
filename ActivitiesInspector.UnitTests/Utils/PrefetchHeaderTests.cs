using Activities_Inspector.Utils;
using System;
using Xunit;
using PfVersion = Activities_Inspector.Utils.Version;

namespace ActivitiesInspector.UnitTests.Utils
{
    public class PrefetchHeaderTests
    {
        [Theory]
        [InlineData(17, PfVersion.WinXpOrWin2K3)]
        [InlineData(23, PfVersion.VistaOrWin7)]
        [InlineData(26, PfVersion.Win8xOrWin2012x)]
        [InlineData(30, PfVersion.Win10)]
        [InlineData(31, PfVersion.Win11)]
        public void Version_Maps_Correctly(int rawVersion, PfVersion expected)
        {
            var raw = new byte[84];
            Buffer.BlockCopy(BitConverter.GetBytes(rawVersion), 0, raw, 0, 4);

            var header = new Header(raw);

            Assert.Equal(expected, header.Version);
        }
    }
}
