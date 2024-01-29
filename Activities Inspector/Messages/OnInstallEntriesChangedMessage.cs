using ProgettoInformaticaForense_Argentieri.Models;
using System.Collections.Generic;

namespace ProgettoInformaticaForense_Argentieri.Messages
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
