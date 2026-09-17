using Activities_Inspector.Constants;
using System;
using System.Collections.Generic;

namespace Activities_Inspector.Models
{
    public class ExtensionBlockBEEF0004 : ExtensionBlock
    {
        public DateTime CreationDate { get; protected set; }
        public DateTime AccessedDate { get; protected set; }
        public ushort LongNameSize { get; protected set; }
        public string LongName { get; protected set; }
        public string LocalizedName { get; protected set; }
        public ExtensionBlockBEEF0004(byte[] buf, int offset) : base(buf, offset)
        {
            int off = 0x08;

            if (ExtensionVersion >= 0x03)
            {
                CreationDate = unpack_dosdate(off);
                off += 4;
                AccessedDate = unpack_dosdate(off);
                off += 4;
                off += 2; 
            }
            else
            {
                CreationDate = DateTime.MinValue;
                AccessedDate = DateTime.MinValue;
            }

            if (ExtensionVersion >= 0x07)
            {
                off += 2;
                off += 8;
                off += 8;
            }

            if (ExtensionVersion >= 0x03)
            {
                LongNameSize = unpack_word(off);
                off += 2;
            }

            if (ExtensionVersion >= 0x09)
                off += 4; 

            if (ExtensionVersion >= 0x08)
                off += 4; 

            if (ExtensionVersion >= 0x03)
            {
                LongName = unpack_wstring(off);
                off += 2 * (LongName.Length + 1);
            }
            if (ExtensionVersion >= 0x03 && ExtensionVersion < 0x07 && LongNameSize > 0)
            {
                LocalizedName = unpack_string(off);
            }
            else if (ExtensionVersion >= 0x07 && LongNameSize > 0)
            {
                LocalizedName = unpack_wstring(off);
            }
            else
            {
                LocalizedName = string.Empty;
            }
        }

        public override IDictionary<string, string> GetAllProperties()
        {
            var ret = base.GetAllProperties();
            AddPairIfNotNull(ret, AppConstants.ShellItems.CreationDate, CreationDate);
            AddPairIfNotNull(ret, AppConstants.ShellItems.AccessedDate, AccessedDate);
            AddPairIfNotNull(ret, AppConstants.ShellItems.LongName, LongName);
            AddPairIfNotNull(ret, AppConstants.ShellItems.LocalizedName, LocalizedName);
            return ret;
        }
    }
}