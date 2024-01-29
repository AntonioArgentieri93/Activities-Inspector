using System;

namespace ProgettoInformaticaForense_Argentieri.Models
{
    public class LogoffEntry : LogEntry
    {
        public DateTime TimeGenerated { get; set; }

        public LogoffEntry(string index, DateTime timeGenerated) : base(index)
        {
            TimeGenerated = timeGenerated;
        }
    }
}