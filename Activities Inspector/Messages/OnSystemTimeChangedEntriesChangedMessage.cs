using Activities_Inspector.Models;
using System.Collections.Generic;

namespace Activities_Inspector.Messages
{
    public class OnSystemTimeChangedEntriesChangedMessage
    {
        public List<SystemTimeChangedEntry> NewTimeChangedEntries { get; }
        public IReadOnlyList<IntegrityRecord> Manifest { get; }

        public OnSystemTimeChangedEntriesChangedMessage(List<SystemTimeChangedEntry> newTimeChangedEntries,
            IReadOnlyList<IntegrityRecord> manifest = null)
        {
            NewTimeChangedEntries = newTimeChangedEntries;
            Manifest = manifest ?? new List<IntegrityRecord>();
        }
    }
}
