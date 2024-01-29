using ProgettoInformaticaForense_Argentieri.Models;
using System.Collections.Generic;

namespace ProgettoInformaticaForense_Argentieri.Messages
{
    public class OnSystemTimeChangedEntriesChangedMessage
    {
        public List<SystemTimeChangedEntry> NewTimeChangedEntries { get; }

        public OnSystemTimeChangedEntriesChangedMessage(List<SystemTimeChangedEntry> newTimeChangedEntries)
        {
            NewTimeChangedEntries = newTimeChangedEntries;
        }
    }
}
