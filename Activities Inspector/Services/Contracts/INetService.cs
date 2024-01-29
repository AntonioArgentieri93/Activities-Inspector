using System.Collections.Generic;

namespace ProgettoInformaticaForense_Argentieri.Services
{
    public interface INetService
    {
        IEnumerable<string> GetAvailablePrivateIPs();
        string GetPublicIPAddress();
    }
}
