using Activities_Inspector.Models;
using System.Collections.Generic;

namespace Activities_Inspector.Messages
{
    public class OnShellBagEntriesChangedMessage
    {
        public List<ShellBagEntry> NewShellBagEntries { get; }
        public bool IsPartial { get; }
        public IReadOnlyList<IntegrityRecord> Manifest { get; }
        public string Source { get; }

        public OnShellBagEntriesChangedMessage(List<ShellBagEntry> newShellBagEntries, bool isPartial = false,
            IReadOnlyList<IntegrityRecord> manifest = null, string source = null)
        {
            NewShellBagEntries = newShellBagEntries;
            IsPartial = isPartial;
            Manifest = manifest ?? new List<IntegrityRecord>();
            Source = source;
        }
    }
}
