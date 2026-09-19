using Activities_Inspector.Models;
using System.Collections.Generic;

namespace Activities_Inspector.Messages
{
    public class OnPrefetchInfoEntriesChangedMessage
    {
        public List<PrefetchInfoEntry> NewPrefetchInfoEntries { get; }
        public IReadOnlyList<IntegrityRecord> Manifest { get; }
        public string Source { get; }

        public OnPrefetchInfoEntriesChangedMessage(List<PrefetchInfoEntry> newPrefetchInfoEntries,
            IReadOnlyList<IntegrityRecord> manifest = null, string source = null)
        {
            NewPrefetchInfoEntries = newPrefetchInfoEntries;
            Manifest = manifest ?? new List<IntegrityRecord>();
            Source = source;
        }
    }
}
