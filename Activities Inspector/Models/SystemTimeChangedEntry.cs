using System;

namespace ProgettoInformaticaForense_Argentieri.Models
{
    public class SystemTimeChangedEntry : Entry
    {
        public string AccountName { get; set; }
        public string TimeGenerated { get; set; }
        public string OldTime { get; set; }
        public string NewTime { get; set; }

        public SystemTimeChangedEntry(string accountName, string timeGenerated, string oldTime, string newTime)
        {
            AccountName = accountName;
            TimeGenerated = timeGenerated;
            OldTime = oldTime;
            NewTime = newTime;
        }
    }
}
