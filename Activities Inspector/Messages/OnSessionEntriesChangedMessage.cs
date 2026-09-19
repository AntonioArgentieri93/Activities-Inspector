using Activities_Inspector.Models;
using System.Collections.Generic;

namespace Activities_Inspector.Messages
{
    public class OnSessionEntriesChangedMessage
    {
        public List<SessionEntry> NewSessionEntries { get; }
        public IReadOnlyList<IntegrityRecord> Manifest { get; }

        public OnSessionEntriesChangedMessage(List<SessionEntry> newSessionEntries,
            IReadOnlyList<IntegrityRecord> manifest = null)
        {
            NewSessionEntries = newSessionEntries;
            Manifest = manifest ?? new List<IntegrityRecord>();
        }
    }
}
