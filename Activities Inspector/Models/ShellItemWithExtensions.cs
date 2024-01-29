using System.Collections.Generic;

namespace ProgettoInformaticaForense_Argentieri.Models
{
    public class ShellItemWithExtensions : ShellItem
    {
        public List<IExtensionBlock> ExtensionBlocks { get; private set; }

        public ShellItemWithExtensions(byte[] buf) : base(buf)
        {
            ExtensionBlocks = new List<IExtensionBlock>();
        }

        public override IDictionary<string, string> GetAllProperties()
        {
            var ret = base.GetAllProperties();
            foreach (IExtensionBlock block in ExtensionBlocks)
            {
                var props = block.GetAllProperties();
            }
            AddPairIfNotNull(ret, "ExtensionBlockCount", ExtensionBlocks.Count);
            return ret;
        }
    }
}
