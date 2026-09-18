using System.Collections.Generic;

namespace Activities_Inspector.Models
{
    public class ShellBagsResult
    {
        public List<IShellItem> Items { get; }
        public bool IsPartial { get; }
        public IReadOnlyList<IntegrityRecord> Manifest { get; }

        public ShellBagsResult(List<IShellItem> items, bool isPartial, IReadOnlyList<IntegrityRecord> manifest = null)
        {
            Items = items;
            IsPartial = isPartial;
            Manifest = manifest ?? new List<IntegrityRecord>();
        }
    }
}
