using Activities_Inspector.Models;
using System.Collections.Generic;

namespace Activities_Inspector.Messages
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
