using Activities_Inspector.Models;
using System.Collections.Generic;

namespace Activities_Inspector.Messages
{
    public class OnUsageInfosChangedMessage
    {
        public List<UsageInfo> NewInfos { get; }
        public IReadOnlyList<IntegrityRecord> Manifest { get; }
        public string Source { get; }

        public OnUsageInfosChangedMessage(List<UsageInfo> newInfos,
            IReadOnlyList<IntegrityRecord> manifest = null, string source = null)
        {
            NewInfos = newInfos;
            Manifest = manifest ?? new List<IntegrityRecord>();
            Source = source;
        }
    }
}
