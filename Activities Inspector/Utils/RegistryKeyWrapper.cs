using Microsoft.Win32;
using System;
using System.Linq;

namespace Activities_Inspector.Utils
{
    public class RegistryKeyWrapper
    {
        public string RegistryUser { get; internal set; }
        public string RegistrySID { get; internal set; }
        public string RegistryPath { get; internal set; }
        public byte[] Value { get; }

        private DateTime slotModifiedDate = DateTime.MinValue;
        public DateTime SlotModifiedDate
        {
            get => slotModifiedDate == DateTime.MinValue ? DateTime.MinValue : TimeZoneInfo.ConvertTimeToUtc(slotModifiedDate);
            internal set => slotModifiedDate = value;
        }

        private DateTime lastRegistryWriteDate;
        public DateTime LastRegistryWriteDate
        {
            get => lastRegistryWriteDate == DateTime.MinValue ? DateTime.MinValue : TimeZoneInfo.ConvertTimeToUtc(lastRegistryWriteDate);
            internal set => lastRegistryWriteDate = value;
        }

        /// <summary>LastWriteTime (ora locale) della chiave BagMRU di questo elemento; null se non disponibile.</summary>
        public DateTime? KeyLastWriteTime => lastRegistryWriteDate == DateTime.MinValue ? (DateTime?)null : lastRegistryWriteDate;

        /// <summary>
        /// Ultima interazione (ora locale): LastWriteTime della chiave genitore, solo se questo elemento è in
        /// posizione 0 nel suo MRUListEx (cartella più recente). Per le altre posizioni la data non è documentata → null.
        /// </summary>
        public DateTime? LastInteracted { get; internal set; }

        public string ShellbagPath { get; internal set; }
        public RegistryKeyWrapper Parent { get; }

        public RegistryKeyWrapper(byte[] value)
        {
            this.Value = value;
            RegistryUser = string.Empty;
            RegistryPath = string.Empty;
            RegistrySID = string.Empty;
            ShellbagPath = string.Empty;
        }

        public RegistryKeyWrapper(byte[] value, string registryUser, string registryPath) : this(value)
        {
            this.RegistryUser = registryUser;
            this.RegistryPath = registryPath;
        }

        /// <summary>
        /// Adapts a ShellBag RegistryKey to a common standard for retrieval of important information independent of key retrieval methodologies
        /// </summary>
        /// <param name="registryKey">A Registry Key associated with a Shellbag, retrieved via Win32 API </param>
        /// <param name="keyValue">The Value of a Registry key containing Shellbag information. Found in the Parent of the registryKey being inspected</param>
        /// <param name="parent">The parent of the currently inspected registryKey. Can be null.</param>
        public RegistryKeyWrapper(Microsoft.Win32.RegistryKey registryKey, byte[] keyValue, RegistryKeyWrapper parent = null) : this(keyValue)
        {
            Parent = parent;
            RegistryPath = registryKey.Name;
            AdaptWin32Key(registryKey);
        }

        /// <summary>
        /// Adapts a ShellBag RegistryKey to a common standard for retrieval of important information independent of key retrieval methodologies
        /// </summary>
        /// <param name="registryKey">A Registry Key associated with a Shellbag, retrieved from a offline registry reader API</param>
        /// <param name="keyValue">The Value of a Registry key containing Shellbag information. Found in the Parent of the registryKey being inspected</param>
        /// <param name="parent">The parent of the currently inspected registryKey. Can be null.</param>
        public RegistryKeyWrapper(global::Registry.Abstractions.RegistryKey registryKey, byte[] keyValue, global::Registry.RegistryHive hive, RegistryKeyWrapper parent = null) : this(keyValue)
        {
            Parent = parent;
            RegistryPath = registryKey.KeyPath;
            AdaptOfflineKey(registryKey, hive);
        }

        private void AdaptWin32Key(Microsoft.Win32.RegistryKey registryKey)
        {
            //obtain SID and Username(?)

            //HKEY USERS registry is {hivename}\UserSID\....
            string UserSID = registryKey.Name.Split('\\')[1];

            // "_classes" is actually just a user's usrclass.dat, not a seperate user.
            UserSID = UserSID.ToUpper().Replace("_CLASSES", "");
            RegistrySID = UserSID;

            //if we dont know the username, default to the SID.
            RegistryUser = RegistrySID;

            //obtain NodeSlot (Shellbag Path in registry)
            SlotModifiedDate = DateTime.MinValue;
            ShellbagPath = string.Empty;
            try
            {
                int slot = 0;
                var value = registryKey.GetValue("NodeSlot");

                if (value != null)
                {
                    slot = (int)value;
                }

                ShellbagPath = string.Format("{0}{1}\\{2}", registryKey.Name.Substring(0, registryKey.Name.IndexOf("BagMRU", StringComparison.Ordinal)), "Bags", slot);

                if (registryKey.Name.StartsWith("HKEY_USERS"))
                {
                    SlotModifiedDate = RegistryHelper.GetDateModified(RegistryHive.Users, ShellbagPath.Replace("HKEY_USERS\\", "")) ?? DateTime.MinValue;
                }
            }
            catch (Exception)
            { }

            //obtain the date the registry last wrote this key
            LastRegistryWriteDate = RegistryHelper.GetDateModified(RegistryHive.Users, registryKey.Name.Replace("HKEY_USERS\\", "")) ?? DateTime.MinValue;

        }

        private void AdaptOfflineKey(global::Registry.Abstractions.RegistryKey registryKey, global::Registry.RegistryHive hive)
        {
            // Offline il primo segmento del KeyPath è il nome della chiave radice dell'hive (es. "ROOT"),
            // non un SID: utente e SID sono impostati dal reader (cartella del profilo).
            RegistrySID = string.Empty;
            RegistryUser = string.Empty;

            //obtain NodeSlot (Shellbag Path in registry)
            SlotModifiedDate = DateTime.MinValue;
            LastRegistryWriteDate = DateTime.MinValue;
            ShellbagPath = string.Empty;
            try
            {
                var nodeSlot = registryKey.Values.FirstOrDefault(kv => kv.ValueName.Equals("NodeSlot"));
                if (nodeSlot != null)
                {
                    ShellbagPath = string.Format("{0}{1}\\{2}", registryKey.KeyPath.Substring(0, registryKey.KeyPath.IndexOf("BagMRU", StringComparison.Ordinal)), "Bags", nodeSlot.ValueData);
                    var shellbagKey = hive.GetKey(ShellbagPath);
                    if (shellbagKey?.LastWriteTime != null)
                        SlotModifiedDate = shellbagKey.LastWriteTime.Value.LocalDateTime;
                }
            }
            catch (Exception)
            { }

            //obtain the date the registry last wrote this key
            if (registryKey.LastWriteTime != null)
                LastRegistryWriteDate = registryKey.LastWriteTime.Value.LocalDateTime;
        }

        /// <summary>
        /// Valore numerico (indice del valore figlio) in posizione 0 di un MRUListEx, o null se vuoto/assente.
        /// MRUListEx = sequenza di DWORD, terminata da 0xFFFFFFFF.
        /// </summary>
        internal static int? MostRecentIndex(byte[] mruListEx)
        {
            if (mruListEx == null || mruListEx.Length < 4) return null;
            var first = BitConverter.ToInt32(mruListEx, 0);
            return first < 0 ? (int?)null : first;
        }
    }
}
