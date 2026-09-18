using Activities_Inspector.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Activities_Inspector.Services
{
    public class AuditTrailService : IAuditTrail
    {
        private readonly List<AuditEntry> _entries = new List<AuditEntry>();
        private readonly object _sync = new object();

        public IReadOnlyList<AuditEntry> Entries
        {
            get
            {
                lock (_sync)
                    return _entries.ToList();
            }
        }

        public AuditEntry Record(AuditCategory category, string detail)
        {
            var cleanDetail = (detail ?? string.Empty).Split('\n')[0];

            lock (_sync)
            {
                var sequence = _entries.Count + 1;
                var timestampUtc = DateTime.UtcNow;
                var previous = _entries.Count == 0
                    ? AuditChain.GenesisPreviousHash
                    : _entries[_entries.Count - 1].Hash;

                var entry = new AuditEntry(
                    sequence,
                    timestampUtc,
                    category,
                    cleanDetail,
                    previous,
                    AuditChain.ComputeHash(sequence, timestampUtc, category, cleanDetail, previous));

                _entries.Add(entry);
                return entry;
            }
        }
    }
}
