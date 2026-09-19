using Activities_Inspector.Models;
using System.Collections.Generic;

namespace Activities_Inspector.Messages
{
    public class OnUsbEntriesChangedMessage
    {
        public List<UsbEntry> NewUsbEntries { get; }
        public IReadOnlyList<IntegrityRecord> Manifest { get; }
        public string Source { get; }

        public OnUsbEntriesChangedMessage(List<UsbEntry> newUsbEntries,
            IReadOnlyList<IntegrityRecord> manifest = null, string source = null)
        {
            NewUsbEntries = newUsbEntries;
            Manifest = manifest ?? new List<IntegrityRecord>();
            Source = source;
        }
    }
}
