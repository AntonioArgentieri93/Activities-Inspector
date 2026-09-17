using Activities_Inspector.Models;
using System.Collections.Generic;

namespace Activities_Inspector.Messages
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
