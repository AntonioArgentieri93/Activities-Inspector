using Activities_Inspector.Models;
using System.Collections.Generic;

namespace Activities_Inspector.Messages
{
    public class OnRecentFolderEntriesChangedMessage
    {
        public List<RecentFolderEntry> NewRecentFoldersEntries { get; }
        public IReadOnlyList<IntegrityRecord> Manifest { get; }

        public OnRecentFolderEntriesChangedMessage(List<RecentFolderEntry> newRecentFoldersEntries,
            IReadOnlyList<IntegrityRecord> manifest = null)
        {
            NewRecentFoldersEntries = newRecentFoldersEntries;
            Manifest = manifest ?? new List<IntegrityRecord>();
        }
    }
}
