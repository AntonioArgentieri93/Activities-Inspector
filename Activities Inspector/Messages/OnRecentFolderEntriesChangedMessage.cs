using Activities_Inspector.Models;
using System.Collections.Generic;

namespace Activities_Inspector.Messages
{
    public class OnRecentFolderEntriesChangedMessage
    {
        public List<RecentFolderEntry> NewRecentFoldersEntries { get; }
        public IReadOnlyList<IntegrityRecord> Manifest { get; }
        public string Source { get; }

        public OnRecentFolderEntriesChangedMessage(List<RecentFolderEntry> newRecentFoldersEntries,
            IReadOnlyList<IntegrityRecord> manifest = null, string source = null)
        {
            NewRecentFoldersEntries = newRecentFoldersEntries;
            Manifest = manifest ?? new List<IntegrityRecord>();
            Source = source;
        }
    }
}
