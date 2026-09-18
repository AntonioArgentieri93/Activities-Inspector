using Activities_Inspector.Models;
using System.Collections.Generic;

namespace Activities_Inspector.Services
{
    public interface IAuditTrail
    {
        IReadOnlyList<AuditEntry> Entries { get; }

        AuditEntry Record(AuditCategory category, string detail);
    }
}
