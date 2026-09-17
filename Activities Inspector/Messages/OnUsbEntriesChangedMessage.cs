using Activities_Inspector.Models;
using System.Collections.Generic;

namespace Activities_Inspector.Messages
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
