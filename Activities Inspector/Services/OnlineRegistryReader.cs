using Microsoft.Win32;
using ProgettoInformaticaForense_Argentieri.Exceptions;
using ProgettoInformaticaForense_Argentieri.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Security.Cryptography;

namespace ProgettoInformaticaForense_Argentieri.Services
{
    public class OnlineRegistryReader : IRegistryReader
    {
        private readonly bool parseAllUsers;

        //known Windows locations of NTUSER.dat and USRCLASS.DAT
        //( see https://support.microsoft.com/en-us/help/3048895/error-occurs-during-desktop-setup-and-desktop-location-is-unavailable)
        private static readonly string[] KNOWN_USER_REGISTRY_FILE_LOCATIONS = {
            @"\ntuser.dat", //ntuser is always in base user directory
            @"\Local Settings\Application Data\Microsoft\Windows\UsrClass.dat",
            @"\AppData\Local\Microsoft\Windows\UsrClass.dat"
        };

        private Dictionary<string, string> sidToUsernameMappings = new Dictionary<string, string>();

        IConfigParser Parser { get; }

        public OnlineRegistryReader(IConfigParser parser, bool parseAllUsers = false)
        {
            this.parseAllUsers = parseAllUsers;
            Parser = parser;
        }


        public List<RegistryKeyWrapper> GetRegistryKeys()
        {
            List<RegistryKeyWrapper> retList = new List<RegistryKeyWrapper>();

            RegistryKey store = Microsoft.Win32.Registry.Users;

            List<RegistryKey> userStores = new List<RegistryKey>();
            List<RegistryKey> currentUserStores = new List<RegistryKey>();

            foreach (string userStoreName in store.GetSubKeyNames())
            {
                var storeKey = store.OpenSubKey(userStoreName);

                if (storeKey == null)
                    continue;

                string userOfStore = FindOnlineUsername(storeKey);

                if (userOfStore.Equals(Environment.UserName, StringComparison.OrdinalIgnoreCase))
                {
                    currentUserStores.Add(storeKey);
                }

                userStores.Add(storeKey);
            }

            if (parseAllUsers)
            {
                retList.AddRange(GetLoggedInUserKeys(userStores));
                retList.AddRange(GetLoggedOffUserKeys());
            }
            else
            {
                retList.AddRange(GetLoggedInUserKeys(currentUserStores));
            }

            return retList;
        }

        private List<RegistryKeyWrapper> GetLoggedInUserKeys(List<RegistryKey> userStores)
        {
            List<RegistryKeyWrapper> retList = new List<RegistryKeyWrapper>();

            //iterate over each user's registry
            foreach (RegistryKey userStore in userStores)
            {
                string userOfStore = FindOnlineUsername(userStore);

                foreach (string location in Parser.GetRegistryLocations())
                {
                    foreach (RegistryKeyWrapper keyWrapper in IterateRegistry(userStore.OpenSubKey(location), location, null))
                    {
                        if (userOfStore != string.Empty)
                        {
                            keyWrapper.RegistryUser = userOfStore;
                        }

                        retList.Add(keyWrapper);
                    }
                }
            }

            return retList;
        }

        /// <summary>
        /// Obtains the Relevant Registry keys from users who are currently not signed in.
        /// Uses the <seealso cref="OfflineRegistryReader"/> to read logged out uses due to their stores not being
        /// loaded into the active registry when not signed in.
        /// <see cref="https://stackoverflow.com/a/11433344"/>
        /// </summary>
        /// <returns>A list of Registry Keys from the offline users on the machine</returns>
        private IEnumerable<RegistryKeyWrapper> GetLoggedOffUserKeys()
        {
            List<RegistryKeyWrapper> retList = new List<RegistryKeyWrapper>();
            HashSet<string> seenRegistryHives = new HashSet<string>();
            try
            {
                //navigate to root users directory
                string rootUserDirectory = Directory.GetParent(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)).ToString();
                string[] userDirectories = Directory.GetDirectories(rootUserDirectory);

                //attempt to navigate into each user directory, cancel and log if we are blocked by security exceptions
                foreach (string userDirectory in userDirectories)
                {
                    string username = userDirectory.Split('\\').Last();
                    try
                    {
                        //check if the files exist in known locations of NTUSER.dat and USRCLASS.DAT 
                        foreach (string userRegistryFilePath in KNOWN_USER_REGISTRY_FILE_LOCATIONS)
                        {
                            string fullFilePath = userDirectory + userRegistryFilePath;
                            if (File.Exists(fullFilePath))
                            {
                                //check if we've seen this file (in case two directories pointed to one file)
                                string hash = GetFileSha256(fullFilePath);
                                if (!seenRegistryHives.Contains(hash))
                                {
                                    seenRegistryHives.Add(hash);
                                    //resolve this user's SID, if its the SID of a user who's logged in, skip.
                                    //todo

                                    // for each of the files that do exist, use the offline parser to parse through the file, return the keys. Log files that arent found.
                                    var offlineReader = new OfflineRegistryReader(Parser, fullFilePath);
                                    var usersKeys = offlineReader.GetRegistryKeys();

                                    //populate the username since we know the user folder it came from
                                    foreach (RegistryKeyWrapper userKey in usersKeys)
                                    {
                                        userKey.RegistryUser = username;
                                    }

                                    retList.AddRange(usersKeys);
                                }
                                else
                                {}
                            }
                            else
                            {}
                        }
                    }
                    catch (Exception)
                    {
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                if (!(ex is UnauthorizedAccessException) && !(ex is SecurityException)) throw;
            }

            return retList;
        }

        private static string GetFileSha256(string filePath)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                using (FileStream stream = File.OpenRead(filePath))
                {
                    byte[] hash = sha256.ComputeHash(stream);
                    var hashString = hash.Aggregate(string.Empty, (current, b) => current + b.ToString("x2"));
                    return hashString;
                }
            }

        }

        private string FindOnlineUsername(RegistryKey userStore)
        {
            string sid = userStore.Name.Split('\\')[1];
            // "_classes" is actually just a user's usrclass.dat, not a seperate user.
            sid = sid.ToUpper().Replace("_CLASSES", "");
            if (sidToUsernameMappings.TryGetValue(sid, out var uName))
            {
                return uName;
            }

            string retval = string.Empty;
            try
            {

                List<string> usernameLocations = Parser.GetUsernameLocations();

                Dictionary<string, int> likelyUsernames = new Dictionary<string, int>();
                foreach (string usernameLocation in usernameLocations)
                {
                    if (userStore.OpenSubKey(usernameLocation) != null)
                    {
                        //based on the values in '...\Explorer\Shell Folders' the [2] value in the string may not always be the username, but it does appear the most.
                        foreach (string value in userStore.OpenSubKey(usernameLocation).GetValueNames())
                        {
                            string valueData = (string)userStore.OpenSubKey(usernameLocation).GetValue(value);
                            string[] pathParts = valueData.Split('\\');
                            if (pathParts.Length > 2)
                            {
                                string username = pathParts[2]; //usually in the form of C:\Users\username
                                if (!likelyUsernames.ContainsKey(username))
                                {
                                    likelyUsernames[username] = 1;
                                }
                                else
                                {
                                    likelyUsernames[username]++;
                                }
                            }
                        }
                    }
                    else
                    {
                        return retval;
                    }
                }

                //most occurred value is probably the username.
                if (likelyUsernames.Count >= 1)
                {
                    retval = likelyUsernames.OrderByDescending(pair => pair.Value).First().Key;
                }

                //add this to our existing list for easy lookup for later potential user hives
                if (retval != null && retval != string.Empty)
                {
                    sidToUsernameMappings.Add(sid, retval);
                }
            }
            catch (Exception)
            { }

            return retval;
        }

        /// <summary>
        /// Recursively iterates over the a registry key and its subkeys for enumerating all values of the keys and subkeys
        /// </summary>
        /// <param name="rk">The root registry key to start iterating over</param>
        /// <param name="subKey">the path of the first subkey under the root key</param>
        /// <param name="parent">The Parent Key of the Registry Key currently being iterated. Can be null</param>
        /// <returns></returns>
        static List<RegistryKeyWrapper> IterateRegistry(RegistryKey rk, string subKey, RegistryKeyWrapper parent)
        {
            List<RegistryKeyWrapper> retList = new List<RegistryKeyWrapper>();
            if (rk == null)
            {
                return retList;
            }

            string[] subKeys = rk.GetSubKeyNames();
            string[] values = rk.GetValueNames();

            foreach (string valueName in subKeys)
            {
                if (valueName.ToUpper() == "ASSOCIATIONS")
                {
                    continue;
                }

                string sk = getSubkeyString(subKey, valueName);
                RegistryKey rkNext;
                try
                {
                    rkNext = rk.OpenSubKey(valueName);
                }
                catch (SecurityException)
                {
                    continue;
                }

                RegistryKeyWrapper rkNextWrapper = null;

                //shellbags only have their numerical identifer for the value name, not a shellbag otherwise
                bool isNumeric = int.TryParse(valueName, out _);
                if (isNumeric)
                {
                    try
                    {
                        byte[] byteVal = (byte[])rk.GetValue(valueName);
                        rkNextWrapper = new RegistryKeyWrapper(rkNext, byteVal, parent);
                        retList.Add(rkNextWrapper);
                    }
                    catch (OverrunBufferException)
                    { }
                    catch (Exception)
                    { }
                }

                retList.AddRange(IterateRegistry(rkNext, sk, rkNextWrapper));
            }

            return retList;

        }

        static string getSubkeyString(string subKey, string addOn)
        {
            return string.Format("{0}{1}{2}", subKey, subKey.Length == 0 ? "" : @"\", addOn);
        }
    }
}
