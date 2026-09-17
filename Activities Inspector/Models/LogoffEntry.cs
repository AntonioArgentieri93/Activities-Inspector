using System;

namespace Activities_Inspector.Models
{
    public class LogoffEntry : LogEntry
    {
        public DateTime TimeGenerated { get; set; }

        public LogoffEntry(string index, DateTime timeGenerated) : base(index)
        {
            TimeGenerated = timeGenerated;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj)) return true;
            if (obj is LogoffEntry other)
            {
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