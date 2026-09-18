using Activities_Inspector.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;

namespace Activities_Inspector.Utils
{
    public static class Helpers
    {
        public static string GetDescriptionFromEnumValue(Enum value)
        {
            var attribute = value.GetType()
                .GetField(value.ToString())
                .GetCustomAttributes(typeof(DescriptionAttribute), false)
                .SingleOrDefault() as DescriptionAttribute;
            return attribute == null ? value.ToString() : attribute.Description;
        }

        public static IEnumerable<IEventRecord> GetLogEntries(string logName)
        {
            using var eventLog = new EventLog { Log = logName };
            foreach (EventLogEntry entry in eventLog.Entries)
            {
                yield return new LiveEventRecord(entry);
            }
        }
    }
}