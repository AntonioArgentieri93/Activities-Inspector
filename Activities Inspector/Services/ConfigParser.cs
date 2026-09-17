using Microsoft.Win32;
using Newtonsoft.Json;
using Activities_Inspector.Constants;
using Activities_Inspector.Models;
using Activities_Inspector.Utils;
using Activities_Inspector.Utils.JSON;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Activities_Inspector.Services
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

        private void UpdateKnownGUIDS(string guidsFile)
        {
            if (guidsFile.Equals(string.Empty))
                return;

            IList<GUIDPair> guidPairs = new List<GUIDPair>();

            var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(AppConstants.Assembly.Namespace + guidsFile);

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

            var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(AppConstants.Assembly.Namespace + file);

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

                var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(AppConstants.Assembly.Namespace + OSRegistryFile);

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
            List<string> list = new List<string>();
            list.Add(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Shell Folders");
            return list;
        }

        private string getLiveOSVersion()
        {
            var registryKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey("Software\\Microsoft\\Windows NT\\CurrentVersion");
            return (string)registryKey.GetValue("productName");
        }


        public static IList<RegistryLocations> GetDefaultRegistryLocations()
        {
            IList<RegistryLocations> retval = new List<RegistryLocations>();
            try
            {
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

        private static bool IsValidConfigFile<T>(string location)
        {
            if (File.Exists(location))
            {
                string json = File.ReadAllText(location);
                try
                {
                    JsonConvert.DeserializeObject<T>(json);
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