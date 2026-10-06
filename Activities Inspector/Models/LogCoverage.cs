using System;

namespace Activities_Inspector.Models
{
    /// <summary>
    /// Periodo effettivamente coperto da un registro eventi. Un registro ruotato o cancellato contiene solo gli
    /// eventi recenti: senza questa indicazione l'assenza di accessi in un periodo non è distinguibile dall'assenza
    /// di dati.
    /// </summary>
    public class LogCoverage
    {
        public LogCoverage(string logName, DateTime? from, DateTime? to, int eventCount)
        {
            LogName = logName;
            From = from;
            To = to;
            EventCount = eventCount;
        }

        public string LogName { get; }
        public DateTime? From { get; }
        public DateTime? To { get; }
        public int EventCount { get; }

        public static LogCoverage FromTimes(string logName, System.Collections.Generic.IEnumerable<DateTime> times)
        {
            DateTime? min = null, max = null;
            var count = 0;
            foreach (var t in times)
            {
                count++;
                if (!min.HasValue || t < min.Value) min = t;
                if (!max.HasValue || t > max.Value) max = t;
            }

            return new LogCoverage(logName, min, max, count);
        }

        /// <summary>Es. "Registro Security: 20481 eventi dal 04/10/2026 14:36:51 al 06/10/2026 22:39:50".</summary>
        public string ToText()
        {
            if (!From.HasValue || !To.HasValue)
                return $"Registro {LogName}: nessun evento";

            const string fmt = "dd/MM/yyyy HH:mm:ss";
            return $"Registro {LogName}: {EventCount} eventi dal {From.Value.ToString(fmt)} al {To.Value.ToString(fmt)}";
        }
    }
}
