using ProgettoInformaticaForense_Argentieri.Models;
using System.Collections.Generic;

namespace ProgettoInformaticaForense_Argentieri.Messages
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
