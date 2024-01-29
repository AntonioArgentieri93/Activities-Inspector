using System.Collections.Generic;

namespace ProgettoInformaticaForense_Argentieri.Models
{
    public class Group
    {
        public IntervalEntry Interval { get; set; }
        public List<PrefetchInfoEntry> PrefetchEntries { get; set; }

        public Group(IntervalEntry interval, List<PrefetchInfoEntry> preeftchEntries)
        {
            Interval = interval;
            PrefetchEntries = preeftchEntries;
        }
    }
}
