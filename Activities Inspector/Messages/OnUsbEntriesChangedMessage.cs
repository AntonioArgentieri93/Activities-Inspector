using ProgettoInformaticaForense_Argentieri.Models;
using System.Collections.Generic;

namespace ProgettoInformaticaForense_Argentieri.Messages
{
    public class OnUsbEntriesChangedMessage
    {
        public List<UsbEntry> NewUsbEntries { get; }

        public OnUsbEntriesChangedMessage(List<UsbEntry> newUsbEntries)
        {
            NewUsbEntries = newUsbEntries;
        }
    }
}
