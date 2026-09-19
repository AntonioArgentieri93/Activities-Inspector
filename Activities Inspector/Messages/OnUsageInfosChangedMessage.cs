using Activities_Inspector.Models;
using System.Collections.Generic;

namespace Activities_Inspector.Messages
{
    public class OnUsageInfosChangedMessage
    {
        public List<UsageInfo> NewInfos { get; }
        public IReadOnlyList<IntegrityRecord> Manifest { get; }

        public OnUsageInfosChangedMessage(List<UsageInfo> newInfos,
            IReadOnlyList<IntegrityRecord> manifest = null)
        {
            NewInfos = newInfos;
            Manifest = manifest ?? new List<IntegrityRecord>();
        }
    }
}
