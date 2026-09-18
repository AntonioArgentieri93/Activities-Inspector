using System;
using System.Diagnostics;
using System.Linq;

namespace Activities_Inspector.Models
{
    public interface IEventRecord
    {
        int EventId { get; }
        string Source { get; }
        DateTime TimeGenerated { get; }
        string MachineName { get; }
        string[] ReplacementStrings { get; }
        short CategoryNumber { get; }
        EventLogEntryType EntryType { get; }
    }

    public sealed class LiveEventRecord : IEventRecord
    {
        private readonly EventLogEntry _entry;

        public LiveEventRecord(EventLogEntry entry)
        {
            _entry = entry ?? throw new ArgumentNullException(nameof(entry));
        }

#pragma warning disable CS0618 // Unico punto che legge EventID obsoleto: serve l'ID puro, non InstanceId (vedi nota in UsageLogTimeService).
        public int EventId => _entry.EventID;
#pragma warning restore CS0618

        public string Source => _entry.Source;
        public DateTime TimeGenerated => _entry.TimeGenerated;
        public string MachineName => _entry.MachineName;
        public string[] ReplacementStrings => _entry.ReplacementStrings.Cast<string>().ToArray();
        public short CategoryNumber => _entry.CategoryNumber;
        public EventLogEntryType EntryType => _entry.EntryType;
    }
}
