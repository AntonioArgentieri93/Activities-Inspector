using Activities_Inspector.Constants;
using Activities_Inspector.Utils;
using System.Collections.Generic;

namespace Activities_Inspector.Models
{
    public class ShellItem0x4D : ShellItem0x40
    {
        public string GUID { get; protected set; }

        public override string TypeName { get => "Network Location - NetworkPlaces"; }
        public override string Name
        {
            get
            {
                if ((Type & 0x0F) == 0x0D)
                {
                    if (KnownGuids.dict.ContainsKey(GUID))
                    {
                        return string.Format("{{{0}}}", KnownGuids.dict[GUID]);
                    }
                    else
                    {
                        return string.Format("{{{0}}}", GUID);
                    }
                }
                else
                {
                    return Location;
                }
            }
        }

        public ShellItem0x4D(byte[] buf) : base(buf)
        {
            GUID = unpack_guid(0x04);
        }

        public override IDictionary<string, string> GetAllProperties()
        {
            var ret = base.GetAllProperties();
            AddPairIfNotNull(ret, AppConstants.ShellItems.Guid, GUID);
            return ret;
        }
    }
}