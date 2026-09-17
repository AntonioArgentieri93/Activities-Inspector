using System.Collections.Generic;

namespace Activities_Inspector.Services
{
    public interface INetService
    {
        IEnumerable<string> GetAvailablePrivateIPs();
        string GetPublicIPAddress();
    }
}
