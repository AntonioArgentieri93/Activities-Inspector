using ProgettoInformaticaForense_Argentieri.Utils;
using System;
using System.Collections.Generic;

namespace ProgettoInformaticaForense_Argentieri.Models
{
    public class ShellItem0x74 : ShellItemWithExtensions
    {

        protected ExtensionBlockBEEF0004 ExtensionBlock { get; }

        public uint Signature { get; protected set; }
        public FILEENTRY_FRAGMENT SubItem { get; protected set; }
        public string DelegateItemIdentifier { get; protected set; }
        public string ItemClassIdentifier { get; protected set; }
        public override string TypeName { get => "Delegate Item"; }
        public override string Name
        {
            get
            {
                if(ExtensionBlock.LongName == null) return SubItem.short_name;
                if (ExtensionBlock.LongName.Length > 0)
                    return ExtensionBlock.LongName;
                return SubItem.short_name;
            }
        }

        public override DateTime ModifiedDate
        {
            get
            {
                return SubItem.ModifiedDate;
            }
        }
        public override DateTime CreationDate
        {
            get
            {
                if (ExtensionBlock != null)
                    return ExtensionBlock.CreationDate;
                return base.CreationDate;
            }
        }
        public override DateTime AccessedDate
        {
            get
            {
                if (ExtensionBlock != null)
                    return ExtensionBlock.AccessedDate;
                return base.AccessedDate;
            }
        }

        public ShellItem0x74(byte[] buf)
            : base(buf)
        {
            Signature = unpack_dword(0x06);

            int off = 0x0A;
            SubItem = new FILEENTRY_FRAGMENT(buf, offset + off, this, 0x04);
            off += SubItem.Size;

            off += 2;

            DelegateItemIdentifier = unpack_guid(off);
            off += 16;
            ItemClassIdentifier = unpack_guid(off);
            off += 16;
            ExtensionBlock = new ExtensionBlockBEEF0004(buf, offset + off);
            ExtensionBlocks.Add(ExtensionBlock);            
            
        }

        public override IDictionary<string, string> GetAllProperties()
        {
            var ret = base.GetAllProperties();
            AddPairIfNotNull(ret, Constants.SIGNATURE, Signature);
            AddPairIfNotNull(ret, Constants.DELEGATE_ITEM_ID, DelegateItemIdentifier);
            AddPairIfNotNull(ret, Constants.ITEM_CLASS_ID, ItemClassIdentifier);
            return ret;
        }
    }
}