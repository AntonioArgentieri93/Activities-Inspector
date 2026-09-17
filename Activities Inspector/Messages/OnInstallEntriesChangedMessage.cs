using Activities_Inspector.Models;
using System.Collections.Generic;

namespace Activities_Inspector.Messages
{
    public class OnInstallEntriesChangedMessage
    {
        public List<InstallEntry> NewInstallEntries { get; }

        public OnInstallEntriesChangedMessage(List<InstallEntry> newInstallEntries)
        {
            NewInstallEntries = newInstallEntries;
        }
    }
}
