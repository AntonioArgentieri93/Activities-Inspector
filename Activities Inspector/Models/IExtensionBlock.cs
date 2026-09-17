using System.Collections.Generic;

namespace Activities_Inspector.Models
{
    public interface IExtensionBlock
    {
        ushort Size { get; }
        ushort ExtensionVersion { get; }
        uint Signature { get; }

        IDictionary<string, string> GetAllProperties();
    }
}
