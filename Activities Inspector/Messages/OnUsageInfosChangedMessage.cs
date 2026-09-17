using Activities_Inspector.Models;
using System.Collections.Generic;

namespace Activities_Inspector.Messages
{
    public class OnUsageInfosChangedMessage
    {
        public List<UsageInfo> NewInfos { get; }

        public OnUsageInfosChangedMessage(List<UsageInfo> newInfos)
        {
            NewInfos = newInfos;
        }
    }
}
