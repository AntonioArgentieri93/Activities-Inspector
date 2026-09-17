using Activities_Inspector.Models;
using System.Collections.Generic;

namespace Activities_Inspector.Messages
{
    public class OnPrefetchInfoEntriesChangedMessage
    {
        public List<PrefetchInfoEntry> NewPrefetchInfoEntries { get; }

        public OnPrefetchInfoEntriesChangedMessage(List<PrefetchInfoEntry> newPrefetchInfoEntries)
        {
            NewPrefetchInfoEntries = newPrefetchInfoEntries;
        }
    }
}
