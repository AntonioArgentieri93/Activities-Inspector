using Activities_Inspector.Models;
using System.Collections.Generic;

namespace Activities_Inspector.Messages
{
    public class OnShellBagEntriesChangedMessage
    {
        public List<ShellBagEntry> NewShellBagEntries { get; }
        public bool IsPartial { get; }

        public OnShellBagEntriesChangedMessage(List<ShellBagEntry> newShellBagEntries, bool isPartial = false)
        {
            NewShellBagEntries = newShellBagEntries;
            IsPartial = isPartial;
        }
    }
}
