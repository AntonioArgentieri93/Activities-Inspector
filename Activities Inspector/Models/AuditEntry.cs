using System;

namespace Activities_Inspector.Models
{
    public enum AuditCategory
    {
        Ricerca,
        Export,
        Report,
        Sorgente
    }

    public class AuditEntry
    {
        public int Sequence { get; }
        public DateTime TimestampUtc { get; }
        public AuditCategory Category { get; }
        public string Detail { get; }
        public string PreviousHash { get; }
        public string Hash { get; }

        public AuditEntry(int sequence, DateTime timestampUtc, AuditCategory category,
            string detail, string previousHash, string hash)
        {
            Sequence = sequence;
            TimestampUtc = timestampUtc;
            Category = category;
            Detail = detail;
            PreviousHash = previousHash;
            Hash = hash;
        }
    }
}
