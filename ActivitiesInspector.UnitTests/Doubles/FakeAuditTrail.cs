using Activities_Inspector.Models;
using Activities_Inspector.Services;
using System;
using System.Collections.Generic;

namespace ActivitiesInspector.UnitTests.Doubles
{
    public sealed class FakeAuditTrail : IAuditTrail
    {
        public readonly List<AuditEntry> Records = new List<AuditEntry>();

        public IReadOnlyList<AuditEntry> Entries => Records;

        public AuditEntry Record(AuditCategory category, string detail)
        {
            var entry = new AuditEntry(Records.Count + 1, DateTime.UtcNow, category, detail, "prev", "hash");
            Records.Add(entry);
            return entry;
        }
    }
}
