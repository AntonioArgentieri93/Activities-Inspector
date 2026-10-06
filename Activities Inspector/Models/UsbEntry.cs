using System;

namespace Activities_Inspector.Models
{
    public class UsbEntry : Entry
    {
        public bool Plugged { get; set; }
        public string DeviceName { get; }

        /// <summary>
        /// ID di istanza del dispositivo (nome della chiave sotto VID_xxxx&amp;PID_xxxx): è il numero di serie se il
        /// dispositivo ne fornisce uno, altrimenti un identificativo generato da Windows (es. "6&amp;12502cca&amp;5&amp;0000").
        /// Identifica la voce (uguaglianza, abbinamento agli eventi di collegamento).
        /// </summary>
        public string InstanceId { get; }

        /// <summary>
        /// Numero di serie fornito dal dispositivo; vuoto se l'ID di istanza è generato da Windows
        /// (regola di Windows: il secondo carattere dell'ID è '&amp;'). Mostrare l'ID generato come "serial number"
        /// farebbe credere a un seriale hardware che non esiste.
        /// </summary>
        public string SerialNumber => HasDeviceSerial(InstanceId) ? InstanceId : string.Empty;

        public string VendorId { get; }
        public string ProductId { get; }
        public string UsbClass { get; }
        public DateTimeOffset? LastConnected { get; set; }
        public DateTimeOffset? LastRemoved { get; set; }

        public UsbEntry(bool plugged, string deviceName, string instanceId, string vendorId, string productId, string usbClass)
        {
            Plugged = plugged;
            DeviceName = deviceName;
            InstanceId = instanceId;
            VendorId = vendorId;
            ProductId = productId;
            UsbClass = usbClass;
        }

        public UsbEntry(bool plugged, string deviceName, string instanceId, string vendorId, string productId,
            string usbClass, DateTimeOffset? lastConnected, DateTimeOffset? lastRemoved)
        {
            Plugged = plugged;
            DeviceName = deviceName;
            InstanceId = instanceId;
            VendorId = vendorId;
            ProductId = productId;
            UsbClass = usbClass;
            LastConnected = lastConnected;
            LastRemoved = lastRemoved;
        }

        /// <summary>true se l'ID di istanza è un numero di serie del dispositivo e non un ID generato da Windows.</summary>
        internal static bool HasDeviceSerial(string instanceId)
            => !string.IsNullOrEmpty(instanceId) && !(instanceId.Length > 1 && instanceId[1] == '&');

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
                    && string.Equals(InstanceId, other.InstanceId, StringComparison.Ordinal);
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
                hash = hash * 31 + (InstanceId != null ? InstanceId.GetHashCode() : 0);
                return hash;
            }
        }
    }
}
