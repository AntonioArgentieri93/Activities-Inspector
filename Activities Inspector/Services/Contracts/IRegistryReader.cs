using Activities_Inspector.Utils;
using System.Collections.Generic;

namespace Activities_Inspector.Services
{
    public interface IRegistryReader
    {
        List<RegistryKeyWrapper> GetRegistryKeys();
    }
}
