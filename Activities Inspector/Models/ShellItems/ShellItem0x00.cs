﻿using ProgettoInformaticaForense_Argentieri.Utils;
using System.Collections.Generic;

namespace ProgettoInformaticaForense_Argentieri.Models
{
    public class ShellItem0x00 : ShellItem
    {
        public string Guid { get; protected set; }
        public override string Name
        {
            get
            {
                if (Size == 0x20)
                {
                    if (KnownGuids.dict.ContainsKey(Guid))
                    {
                        return string.Format("{{{0}}}", KnownGuids.dict[Guid]);
                    }
                    else
                    {
                        return string.Format("{{{0}}}", Guid);
                    }
                }
                else
                {
                    return "??";
                }
            }
        }
        public ShellItem0x00(byte[] buf) : base(buf)
        {
            if (Size == 0x20)
            {
                Guid = unpack_guid(0x0E);
            }
        }

        public override IDictionary<string, string> GetAllProperties()
        {
            var ret = base.GetAllProperties();
            AddPairIfNotNull(ret, Constants.GUID, Guid);
            return ret;
        }
    }
}
