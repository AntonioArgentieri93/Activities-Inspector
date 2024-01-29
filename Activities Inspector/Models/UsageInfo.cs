using System;

namespace ProgettoInformaticaForense_Argentieri.Models
{
    public class UsageInfo : Entry
    {
        public IntervalEntry Interval { get; set; }
        public TimeSpan Duration { get; set; }
        public string MachineName { get; set; }

        public UsageInfo(IntervalEntry interval, TimeSpan duration,string machineName)
        {
            Interval = interval;
            Duration = duration;
            MachineName = machineName;
        }
    }
}
