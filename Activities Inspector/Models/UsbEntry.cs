using System;

namespace ProgettoInformaticaForense_Argentieri.Models
{
    public class UsbEntry : Entry
    {
        public bool Plugged { get; set; }
        public string DeviceName { get; }
        public string SerialNumber { get; }
        public string VendorId { get; }
        public string ProductId { get; }
        public string UsbClass { get; }
        public DateTimeOffset? LastConnected { get; set; }
        public DateTimeOffset? LastRemoved { get; set; }

        public UsbEntry(bool plugged, string deviceName, string serialNumber, string vendorId, string productId, string usbClass)
        {
            Plugged = plugged;
            DeviceName = deviceName;
            SerialNumber = serialNumber;
            VendorId = vendorId;
            ProductId = productId;
            UsbClass = usbClass;
        }

        public UsbEntry(bool plugged, string deviceName, string serialNumber, string vendorId, string productId,
            string usbClass, DateTimeOffset? lastConnected, DateTimeOffset? lastRemoved)
        {
            Plugged = plugged;
            DeviceName = deviceName;
            SerialNumber = serialNumber;
            VendorId = vendorId;
            ProductId = productId;
            UsbClass = usbClass;
            LastConnected = lastConnected;
            LastRemoved = lastRemoved;
        }
    }
}
