using System;

namespace Activities_Inspector.Models
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
            if (ReferenceEquals(this, obj)) return true;
            if (obj is LogOnEntry other)
            {
                // Due record rappresentano lo stesso logon se condividono
                // il Logon ID (Index): è anche la chiave usata per accoppiare
                // logon e logoff. Il solo timestamp non basta, perché due
                // logon distinti possono avvenire nello stesso secondo.
                return string.Equals(Index, other.Index, StringComparison.Ordinal);
            }

            return false;
        }

        public override int GetHashCode()
        {
            return Index != null ? Index.GetHashCode() : 0;
        }
    }
}
