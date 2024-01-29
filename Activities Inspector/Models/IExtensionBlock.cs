using System.Collections.Generic;

namespace ProgettoInformaticaForense_Argentieri.Models
{
    public interface IExtensionBlock
    {
        ushort Size { get; }
        ushort ExtensionVersion { get; }
        uint Signature { get; }

        IDictionary<string, string> GetAllProperties();
    }
}
