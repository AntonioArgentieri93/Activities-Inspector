using Activities_Inspector.Services;
using Xunit;

namespace ActivitiesInspector.UnitTests.Services
{
    public class UsbTrackingIdTests
    {
        [Fact]
        public void Standard_Key_Parses_Vid_Pid()
        {
            Assert.Equal("0781", UsbTrackingService.BuildVendorId(@"VID_0781&PID_5567"));
            Assert.Equal("5567", UsbTrackingService.BuildProductId(@"VID_0781&PID_5567"));
        }

        [Fact]
        public void Missing_Pid_Yields_Empty_ProductId()
        {
            Assert.Equal("0781", UsbTrackingService.BuildVendorId("VID_0781"));
            Assert.Equal(string.Empty, UsbTrackingService.BuildProductId("VID_0781"));
        }

        [Fact]
        public void TryParse_Usb_Path()
        {
            bool ok = UsbTrackingService.TryParseVendorProduct(
                @"USB\VID_0781&PID_5567\ABC123", out string vid, out string pid);

            Assert.True(ok);
            Assert.Equal("0781", vid);
            Assert.Equal("5567", pid);
        }

        [Fact]
        public void TryParse_NonUsb_Path_Fails()
        {
            Assert.False(UsbTrackingService.TryParseVendorProduct(
                @"PCI\VEN_8086&DEV_1234\ABC", out _, out _));
        }

        [Fact]
        public void TryParse_Malformed_Fails_Without_Throwing()
        {
            Assert.False(UsbTrackingService.TryParseVendorProduct(null, out _, out _));
            Assert.False(UsbTrackingService.TryParseVendorProduct(string.Empty, out _, out _));
            Assert.False(UsbTrackingService.TryParseVendorProduct("NOVID", out _, out _));
            Assert.False(UsbTrackingService.TryParseVendorProduct(@"USB\VID_0781", out _, out _));
        }

        [Fact]
        public void Malformed_Keys_Yield_Empty_Without_Throwing()
        {
            Assert.Equal(string.Empty, UsbTrackingService.BuildVendorId("AB"));
            Assert.Equal(string.Empty, UsbTrackingService.BuildProductId("A&B"));
            Assert.Equal(string.Empty, UsbTrackingService.BuildVendorId(null));
            Assert.Equal(string.Empty, UsbTrackingService.BuildProductId(string.Empty));
        }
    }
}
