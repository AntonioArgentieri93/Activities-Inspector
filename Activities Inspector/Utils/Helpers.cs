using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;

namespace ProgettoInformaticaForense_Argentieri.Utility
{
    public class Helpers
    {
        public static string GetDescriptionFromEnumValue(Enum value)
        {
            var attribute = value.GetType()
                .GetField(value.ToString())
                .GetCustomAttributes(typeof(DescriptionAttribute), false)
                .SingleOrDefault() as DescriptionAttribute;
            return attribute == null ? value.ToString() : attribute.Description;
        }

        public static IEnumerable<EventLogEntry> GetLogEntries(string filter)
        {
            var eventLog = new EventLog();
            eventLog.Log = filter;

            foreach (var @event in eventLog.Entries)
            {
                var logEntry = (EventLogEntry)@event;
                yield return logEntry;
            }
        }
    }
}
