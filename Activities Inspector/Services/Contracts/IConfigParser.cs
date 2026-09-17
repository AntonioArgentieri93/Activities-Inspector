using System.Collections.Generic;

namespace Activities_Inspector.Services
{
    public interface IConfigParser
    {
        List<string> GetRegistryLocations();

        List<string> GetUsernameLocations();
    }
}