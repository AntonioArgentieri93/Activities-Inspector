﻿using Microsoft.Win32;
using System;

namespace ProgettoInformaticaForense_Argentieri.Utils
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
        public RegistryKeyWrapper(global::Registry.Abstractions.RegistryKey registryKey, byte[] keyValue, global::Registry.RegistryHiveOnDemand hive, RegistryKeyWrapper parent = null) : this(keyValue)
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
                //int slot = (int)registryKey.GetValue("NodeSlot");

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

        private void AdaptOfflineKey(global::Registry.Abstractions.RegistryKey registryKey, global::Registry.RegistryHiveOnDemand hive)
        {
            //obtain SID and Username(?)

            //HKEY USERS registry is UserSID\....
            string UserSID = registryKey.KeyPath.Split('\\')[0];

            // "_classes" is actually just a user's usrclass.dat, not a seperate user.
            UserSID = UserSID.ToUpper().Replace("_CLASSES", "");
            RegistrySID = UserSID;

            //if we dont know the username, default to the SID. 
            RegistryUser = RegistrySID;

            //obtain NodeSlot (Shellbag Path in registry)
            SlotModifiedDate = DateTime.MinValue;
            LastRegistryWriteDate = DateTime.MinValue;
            ShellbagPath = string.Empty;
            try
            {
                var values = registryKey.Values;
                foreach (global::Registry.Abstractions.KeyValue kv in registryKey.Values)
                {
                    if (kv.ValueName.Equals("NodeSlot"))
                    {
                        string slot = kv.ValueData;
                        ShellbagPath = string.Format("{0}{1}\\{2}", registryKey.KeyPath.Substring(0, registryKey.KeyPath.IndexOf("BagMRU", StringComparison.Ordinal)), "Bags", slot);
                    }
                }
                var shellbagKey = hive.GetKey(ShellbagPath);
                SlotModifiedDate = shellbagKey.LastWriteTime.Value.LocalDateTime;
            }
            catch (Exception)
            { }

            //obtain the date the registry last wrote this key
            LastRegistryWriteDate = registryKey.LastWriteTime.Value.LocalDateTime;
        }
    }
}
