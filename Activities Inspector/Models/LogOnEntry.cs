using System;

namespace ProgettoInformaticaForense_Argentieri.Models
{
    public class LogOnEntry : LogEntry
    {
        public int EventId { get; set; }
        public string MachinName { get; set; }
        public DateTime TimeGenerated { get; set; }
        public string AccountName { get; set; }
        public string DomainName { get; set; }
        public string Group { get; set; }
        public int AccessType { get; set; }
        public string SourceAddress { get; set; }

        public LogOnEntry(int eventId, string machineName, string index, DateTime timeGenerated, string accountName,
            string domainName, string group, int accessType, string sourceAddress) : base(index)
        {
            EventId = eventId;
            MachinName = machineName;
            Index = index;
            TimeGenerated = timeGenerated;
            AccountName = accountName;
            DomainName = domainName;
            Group = group;
            AccessType = accessType;
            SourceAddress = sourceAddress;
        }

        public override bool Equals(object obj)
        {
            if (obj == null) return false;

            var toCompare = (LogOnEntry)obj;

            return this.TimeGenerated == toCompare.TimeGenerated ? true : false;
        }
    }
}
