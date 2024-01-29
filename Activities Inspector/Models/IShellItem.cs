using System;
using System.Collections.Generic;

namespace ProgettoInformaticaForense_Argentieri.Models
{
    public interface IShellItem
    {
        ushort Size { get; }
        byte Type { get; }
        string TypeName { get; }
        string Name { get; }
        DateTime ModifiedDate { get; }
        DateTime AccessedDate { get; }
        DateTime CreationDate { get; }
        IDictionary<string, string> GetAllProperties();

    }
}
