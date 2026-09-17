using Activities_Inspector.ShellBags.ShellBags;
using Activities_Inspector.Utils;
using System;
using Xunit;

namespace ActivitiesInspector.UnitTests.Utils
{
    public class ShellItemFactoryTests
    {
        [Fact]
        public void Unknown_Id_Throws_With_Id_In_Message()
        {
            var raw = new byte[] { 0x03, 0x00, 0xFF };

            var ex = Assert.Throws<Exception>(() => ShellItemFactory.Create(raw));

            Assert.Contains("0xFF", ex.Message);
        }

        [Fact]
        public void Type_0x01_Creates_ShellBag0X01()
        {
            var raw = new byte[16];
            raw[2] = 0x01;

            var item = ShellItemFactory.Create(raw);

            var typed = Assert.IsType<ShellBag0X01>(item);
            Assert.Equal("Control Panel Category", typed.FriendlyName);
        }
    }
}
