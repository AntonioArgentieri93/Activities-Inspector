﻿using ProgettoInformaticaForense_Argentieri.Utils;
using System.Collections.Generic;

namespace ProgettoInformaticaForense_Argentieri.Models
{
    public class ShellItem0x71 : ShellItem
    {
        public string Guid { get; protected set; }
        public byte Flags { get; protected set; }
        public override string TypeName { get => "Control Panel";}
        public override string Name
        {
            get
            {
                if (KnownGuids.dict.ContainsKey(Guid))
                {
                    return string.Format("{{CONTROL PANEL: {0}}}", KnownGuids.dict[Guid]);
                }
                else
                {
                    return string.Format("{{CONTROL PANEL: {0}}}", Guid);
                }
            }
        }
        public ShellItem0x71(byte[] buf)
            : base(buf)
        {
            Guid = unpack_guid(0x0E);
            Flags = unpack_byte(0x03);
        }
        public override IDictionary<string, string> GetAllProperties()
        {
            var ret = base.GetAllProperties();
            AddPairIfNotNull(ret, Constants.GUID, Guid);
            AddPairIfNotNull(ret, Constants.FLAGS, Flags);
            return ret;
        }
    }
}