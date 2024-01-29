using ProgettoInformaticaForense_Argentieri.Models;
using System.Collections.Generic;

namespace ProgettoInformaticaForense_Argentieri.Messages
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
