using Activities_Inspector.Models;
using System.Collections.Generic;

namespace Activities_Inspector.Messages
{
    public class OnPrefetchInfoEntriesChangedMessage
    {
        public List<PrefetchInfoEntry> NewPrefetchInfoEntries { get; }
        public IReadOnlyList<IntegrityRecord> Manifest { get; }

        public OnPrefetchInfoEntriesChangedMessage(List<PrefetchInfoEntry> newPrefetchInfoEntries,
            IReadOnlyList<IntegrityRecord> manifest = null)
        {
            NewPrefetchInfoEntries = newPrefetchInfoEntries;
            Manifest = manifest ?? new List<IntegrityRecord>();
        }
    }
}
