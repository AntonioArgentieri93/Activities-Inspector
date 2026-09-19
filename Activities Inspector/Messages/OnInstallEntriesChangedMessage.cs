using Activities_Inspector.Models;
using System.Collections.Generic;

namespace Activities_Inspector.Messages
{
    public class OnInstallEntriesChangedMessage
    {
        public List<InstallEntry> NewInstallEntries { get; }
        public IReadOnlyList<IntegrityRecord> Manifest { get; }
        public string Source { get; }

        public OnInstallEntriesChangedMessage(List<InstallEntry> newInstallEntries,
            IReadOnlyList<IntegrityRecord> manifest = null, string source = null)
        {
            NewInstallEntries = newInstallEntries;
            Manifest = manifest ?? new List<IntegrityRecord>();
            Source = source;
        }
    }
}
