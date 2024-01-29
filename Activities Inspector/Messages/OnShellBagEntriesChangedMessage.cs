using ProgettoInformaticaForense_Argentieri.Models;
using System.Collections.Generic;

namespace ProgettoInformaticaForense_Argentieri.Messages
{
    public class OnShellBagEntriesChangedMessage
    {
        public List<ShellBagEntry> NewShellBagEntries { get; }

        public OnShellBagEntriesChangedMessage(List<ShellBagEntry> newShellBagEntries)
        {
            NewShellBagEntries = newShellBagEntries;
        }
    }
}
