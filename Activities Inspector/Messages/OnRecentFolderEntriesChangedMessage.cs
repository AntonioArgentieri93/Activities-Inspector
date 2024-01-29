using ProgettoInformaticaForense_Argentieri.Models;
using System.Collections.Generic;

namespace ProgettoInformaticaForense_Argentieri.Messages
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
