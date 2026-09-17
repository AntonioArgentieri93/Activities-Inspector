using Activities_Inspector.Models;
using System.Collections.Generic;

namespace Activities_Inspector.Messages
{
    public class OnRecentFolderEntriesChangedMessage
    {
        public List<RecentFolderEntry> NewRecentFoldersEntries { get; }

        public OnRecentFolderEntriesChangedMessage(List<RecentFolderEntry> newRecentFoldersEntries)
        {
            NewRecentFoldersEntries = newRecentFoldersEntries;
        }
    }
}
