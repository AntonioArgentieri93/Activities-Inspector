using System.Collections.Generic;

namespace Activities_Inspector.Models
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
