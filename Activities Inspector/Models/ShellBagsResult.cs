using System.Collections.Generic;

namespace Activities_Inspector.Models
{
    public class ShellBagsResult
    {
        public List<IShellItem> Items { get; }
        public bool IsPartial { get; }

        public ShellBagsResult(List<IShellItem> items, bool isPartial)
        {
            Items = items;
            IsPartial = isPartial;
        }
    }
}
