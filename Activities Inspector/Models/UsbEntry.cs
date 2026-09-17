using System;

namespace Activities_Inspector.Models
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

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj)) return true;
            if (obj is UsbEntry other)
            {
                // L'identità hardware: stesso dispositivo fisico anche se
                // stato/timestamp differiscono (es. rilevato in più ControlSet).
                // Plugged/LastConnected/LastRemoved sono esclusi anche perché
                // mutabili a runtime (eventi WMI nella UsbViewModel).
                return string.Equals(VendorId, other.VendorId, StringComparison.Ordinal)
                    && string.Equals(ProductId, other.ProductId, StringComparison.Ordinal)
                    && string.Equals(SerialNumber, other.SerialNumber, StringComparison.Ordinal);
            }

            return false;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + (VendorId != null ? VendorId.GetHashCode() : 0);
                hash = hash * 31 + (ProductId != null ? ProductId.GetHashCode() : 0);
                hash = hash * 31 + (SerialNumber != null ? SerialNumber.GetHashCode() : 0);
                return hash;
            }
        }
    }
}
