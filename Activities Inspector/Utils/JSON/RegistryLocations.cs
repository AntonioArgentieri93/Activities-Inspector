using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace ProgettoInformaticaForense_Argentieri.Utils.JSON
{
    public class RegistryLocations
    {

        /// <summary>
        /// Operating System 
        /// </summary>
        [JsonProperty(propertyName: "os", Required = Required.Always)]
        public string OperatingSystem { get; set; }

        [JsonProperty(propertyName: "files", Required = Required.Always)]
        public IList<string> RegistryFilePaths { private get; set; }


        public RegistryLocations(string operatingSystem, IList<string> registryFilePaths)
        {
            OperatingSystem = operatingSystem;
            RegistryFilePaths = registryFilePaths;
        }

        public RegistryLocations()
        {
        }

        /// <summary>
        /// Returns a Dictionary of pairings of Registry files and the registy paths for the shellbags in them.
        /// Example: ("NTUSER.DAT", ["p\a\t\h\1", "p\a\t\h\2"])
        /// </summary>
        /// <returns></returns>
        public IDictionary<string, IList<string>> GetRegistryFilePaths()
        {
            var retDict = new Dictionary<string, IList<string>>();
            foreach (string path in RegistryFilePaths)
            {
                string[] pathSplit = path.Split('\\');
                string regPath = string.Join("\\", pathSplit.AsEnumerable().Skip(1).ToArray());
                if (retDict.ContainsKey(pathSplit.First()))
                {
                    retDict[pathSplit.First()].Add(regPath);
                }
                else
                {
                    retDict.Add(pathSplit.First(), new List<string> { regPath });
                }
            }
            return retDict;
        }

    }
}
