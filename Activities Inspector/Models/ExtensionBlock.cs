using ProgettoInformaticaForense_Argentieri.Constants;
using System.Collections.Generic;

namespace ProgettoInformaticaForense_Argentieri.Models
{
    public class ExtensionBlock : Block, IExtensionBlock
    {

        public virtual ushort Size { get; protected set; }
        public virtual ushort ExtensionVersion { get; protected set; }
        public virtual uint Signature { get; protected set; }


        public ExtensionBlock(byte[] buf, int offset) : base(buf, offset)
        {
            Size = unpack_word(0x00);
            ExtensionVersion = unpack_word(0x02);
            Signature = unpack_dword(0x04);
        }

        public virtual IDictionary<string, string> GetAllProperties()
        {
            SortedDictionary<string, string> properties = new SortedDictionary<string, string>();
            AddPairIfNotNull(properties, AppConstants.ShellItems.Size, Size.ToString("X2"));
            AddPairIfNotNull(properties, AppConstants.ShellItems.ExtensionVersion, ExtensionVersion.ToString());
            AddPairIfNotNull(properties, AppConstants.ShellItems.Signature, Signature.ToString("X4"));
            return properties;
        }
    }
}