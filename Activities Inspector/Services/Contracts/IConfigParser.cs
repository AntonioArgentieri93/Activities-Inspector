using System.Collections.Generic;

namespace ProgettoInformaticaForense_Argentieri.Services
{
    public interface IConfigParser
    {
        List<string> GetRegistryLocations();

        List<string> GetUsernameLocations();
    }
}