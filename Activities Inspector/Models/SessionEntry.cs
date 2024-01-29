using System;

namespace ProgettoInformaticaForense_Argentieri.Models
{
    public class SessionEntry : LogEntry
    {
        public string UserName { get; set; }
        public string Group { get; set; } //Domain 
        public string MachineName { get; set; }
        public DateTime LogOnTime { get; set; }
        public DateTime? LogOffTime { get; set; }
        public TimeSpan? Duration { get; set; }
        public string NetworkAddress { get; set; }
        public string AccessType { get; set; }

        public SessionEntry(string index, string userName, string group, string machineName,
            DateTime logOnTime, DateTime? logOffTime, TimeSpan? duration, string networdAddress, string accessType) : base(index)
        {
            UserName = userName;
            Group = group;
            MachineName = machineName;
            LogOnTime = logOnTime;
            LogOffTime = logOffTime;
            Duration = duration;
            NetworkAddress = networdAddress;
            AccessType = accessType;
        }
    }
}
