using ProgettoInformaticaForense_Argentieri.Models;
using System.Collections.Generic;

namespace ProgettoInformaticaForense_Argentieri.Messages
{
    public class OnSessionEntriesChangedMessage
    {
        public List<SessionEntry> NewSessionEntries { get; }

        public OnSessionEntriesChangedMessage(List<SessionEntry> newSessionEntries)
        {
            NewSessionEntries = newSessionEntries;
        }
    }
}
