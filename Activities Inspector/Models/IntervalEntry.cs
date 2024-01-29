using System;

namespace ProgettoInformaticaForense_Argentieri.Models
{
    public class IntervalEntry
    {
        public DateTime Start { get; set; }
        public DateTime? End { get; set; }

        public IntervalEntry(DateTime start, DateTime? end)
        {
            Start = start;
            End = end;
        }

        public IntervalEntry(DateTime? end)
        {
            End = end;
        }
    }
}
