using Activities_Inspector.Models;
using System.Collections.Generic;

namespace Activities_Inspector.Messages
{
    public class OnUsbEntriesChangedMessage
    {
        public List<UsbEntry> NewUsbEntries { get; }
        public IReadOnlyList<IntegrityRecord> Manifest { get; }

        public OnUsbEntriesChangedMessage(List<UsbEntry> newUsbEntries,
            IReadOnlyList<IntegrityRecord> manifest = null)
        {
            NewUsbEntries = newUsbEntries;
            Manifest = manifest ?? new List<IntegrityRecord>();
        }
    }
}
