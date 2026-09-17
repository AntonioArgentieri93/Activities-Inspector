using Activities_Inspector.Constants;
using System.Collections.Generic;

namespace Activities_Inspector.Models
{
    public class ShellItem0x40 : ShellItem
    {
        public byte Flags { get; protected set; }
        public string Location { get; protected set; }
        public string Description { get; protected set; }
        public string Comments { get; protected set; }
        public override string TypeName { get => "Network Location"; }
        public override string Name => Location;
        public ShellItem0x40(byte[] buf) : base(buf)
        {
            Flags = unpack_byte(0x04);
            Location = unpack_string(0x05);
            int off = 0x05;
            off += Name.Length + 1;
            if ((Flags & 0x80) != 0)
            {
                Description = unpack_string(off);
                off += Description.Length + 1;
            }
            if ((Flags & 0x40) != 0)
            {
                Comments = unpack_string(off);
            }
        }

        public override IDictionary<string, string> GetAllProperties()
        {
            var ret =  base.GetAllProperties();
            AddPairIfNotNull(ret, AppConstants.ShellItems.Flags, Flags);
            AddPairIfNotNull(ret, AppConstants.ShellItems.Location, Location);
            AddPairIfNotNull(ret, AppConstants.ShellItems.Description, Description);
            AddPairIfNotNull(ret, AppConstants.ShellItems.Comments, Comments);
            return ret;
        }
    }
}