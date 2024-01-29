using ProgettoInformaticaForense_Argentieri.Utils;
using System.Collections.Generic;

namespace ProgettoInformaticaForense_Argentieri.Services
{
    public interface IRegistryReader
    {
        List<RegistryKeyWrapper> GetRegistryKeys();
    }
}
