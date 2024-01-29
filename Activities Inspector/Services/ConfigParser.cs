using Microsoft.Win32;
using Newtonsoft.Json;
using ProgettoInformaticaForense_Argentieri.Models;
using ProgettoInformaticaForense_Argentieri.Utils;
using ProgettoInformaticaForense_Argentieri.Utils.JSON;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace ProgettoInformaticaForense_Argentieri.Services
{
    public class ConfigParser : IConfigParser
    {
        private readonly string OSRegistryFile;
        private const string DefaultOsConfig = "defaultOS.json";

        public string OsVersion { private get; set; }

        public ConfigParser(string guidsFile = "", string OSRegistryFile = "", string scriptsFile = "")
        {
            guidsFile = IsValidGuidFile(guidsFile) ? ".Assets.GUIDs.json" : string.Empty;
            OSRegistryFile = IsValidOsFile(OSRegistryFile) ? ".Assets.OS.json" : string.Empty;
            scriptsFile = IsValidScriptFile(scriptsFile) ? ".Assets.Scripts.json" : string.Empty;

            UpdateKnownGUIDS(guidsFile);
            UpdateScripts(scriptsFile);
            OsVersion = getLiveOSVersion();
            this.OSRegistryFile = OSRegistryFile;
        }

        /// <summary>
        /// Overrides (or Adds) existing GUID entries into <see cref="KnownGuids.dict"/>
        /// </summary>
        /// <param name="guidsFile">A JSON file that contains <see cref="GUIDPair"/></param>
        private void UpdateKnownGUIDS(string guidsFile)
        {
            if (guidsFile.Equals(string.Empty))
                return;

            IList<GUIDPair> guidPairs = new List<GUIDPair>();

            var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(Constants.ASSEMBLY_NAMESPACE + guidsFile);

            if (stream == null) return;

            using (var reader = new StreamReader(stream))
            {
                guidPairs = JsonConvert.DeserializeObject<IList<GUIDPair>>(reader.ReadToEnd());
            }

            foreach (GUIDPair pair in guidPairs)
            {
                KnownGuids.dict[pair.getKnownGUID().Key] = pair.getKnownGUID().Value;
            }
        }

        private void UpdateScripts(string file)
        {
            if (file.Equals(string.Empty))
                return;

            IList<DecodedScriptPair> scriptPairs = new List<DecodedScriptPair>();

            var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(Constants.ASSEMBLY_NAMESPACE + file);

            if (stream == null) return;

            using (var reader = new StreamReader(stream))
            {
                scriptPairs = JsonConvert.DeserializeObject<IList<DecodedScriptPair>>(reader.ReadToEnd());
            }

            foreach (DecodedScriptPair pair in scriptPairs)
            {
                ScriptHandler.scripts[pair.getScript().Key] = pair.getScript().Value;
            }
        }

        public List<string> GetRegistryLocations()
        {
            List<string> locations = new List<string>();

            if (OSRegistryFile.Equals(string.Empty))
            {
                GetDefaultRegistryLocations();
            }
            else
            {
                IList<RegistryLocations> registrylocations = new List<RegistryLocations>();

                var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(Constants.ASSEMBLY_NAMESPACE + OSRegistryFile);

                using (var reader = new StreamReader(stream))
                {
                    registrylocations = JsonConvert.DeserializeObject<IList<RegistryLocations>>(reader.ReadToEnd());
                }

                foreach (var regLocation in registrylocations)
                {
                    if (OsVersion.Contains(regLocation.OperatingSystem))
                    {
                        foreach (IList<string> registryPaths in regLocation.GetRegistryFilePaths().Values)
                        {
                            locations.AddRange(registryPaths);
                        }

                        return locations;
                    }
                }
            }

            return locations;
        }

        public List<string> GetUsernameLocations()
        {
            //todo retrieve from OSRegistryFile instead of hardcoded path
            List<string> list = new List<string>();
            // found username retrievalable key-value @ https://stackoverflow.com/a/53585223
            list.Add(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Shell Folders");
            return list;
        }

        private string getLiveOSVersion()
        {
            var registryKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey("Software\\Microsoft\\Windows NT\\CurrentVersion");
            return (string)registryKey.GetValue("productName");
        }


        /// <summary>
        /// Retrieves the default <see cref="RegistryLocations"/> configuration from the embedded resource.
        /// </summary>
        /// <returns>A list of Registry Locations.</returns>
        public static IList<RegistryLocations> GetDefaultRegistryLocations()
        {
            IList<RegistryLocations> retval = new List<RegistryLocations>();
            try
            {
                //internal resource retrieval, see: https://stackoverflow.com/a/3314213
                Assembly assembly = Assembly.GetExecutingAssembly();
                string internalResourcePath = assembly.GetManifestResourceNames().Single(str => str.EndsWith(DefaultOsConfig));
                using (Stream fileStream = assembly.GetManifestResourceStream(internalResourcePath))
                {
                    using (StreamReader reader = new StreamReader(fileStream))
                    {
                        retval = JsonConvert.DeserializeObject<IList<RegistryLocations>>(reader.ReadToEnd());
                    }
                }
            }
            catch (JsonSerializationException)
            {}

            return retval;
        }

        /// <summary>
        /// Checks if a JSON file can be deserialized for a valid known Type
        /// </summary>
        /// <typeparam name="T">The known API Type to deserialize from. See <see cref="IO.Networking.JSON"/></typeparam>
        /// <param name="location">a fully qualified path to a json file</param>
        /// <returns>true, if the file could successfully be deserialized. False otherwise.</returns>
        private static bool IsValidConfigFile<T>(string location)
        {
            if (File.Exists(location))
            {
                string json = File.ReadAllText(location);
                try
                {
                    JsonConvert.DeserializeObject<T>(json);
                    //if we can deserialize, we can use it.
                    return true;
                }
                catch (JsonSerializationException)
                {
                    return false;
                }
            }

            return false;
        }

        private static bool IsValidOsFile(string location)
        {
            return IsValidConfigFile<IList<RegistryLocations>>(location);
        }
        private static bool IsValidGuidFile(string location)
        {
            return IsValidConfigFile<IList<GUIDPair>>(location);
        }

        private static bool IsValidScriptFile(string location)
        {
            return IsValidConfigFile<IList<DecodedScriptPair>>(location);
        }
    }
}
