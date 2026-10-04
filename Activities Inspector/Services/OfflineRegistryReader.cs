using Activities_Inspector.Utils;
using Registry;
using Registry.Abstractions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Activities_Inspector.Services
{
    public class OfflineRegistryReader : IRegistryReader
    {
        IConfigParser Parser { get; }
        private readonly string RegistryFilePath;

        /// <summary>Transaction log applicati all'hive durante l'ultima lettura (vuoto se l'hive era pulito).</summary>
        public IReadOnlyList<string> AppliedLogs { get; private set; } = Array.Empty<string>();

        public OfflineRegistryReader(IConfigParser parser, String registryFilePath)
        {
            Parser = parser;
            RegistryFilePath = registryFilePath;
        }

        public List<RegistryKeyWrapper> GetRegistryKeys()
        {
            List<RegistryKeyWrapper> retList = new List<RegistryKeyWrapper>();
            RegistryHive hive;
            try
            {
                hive = OfflineHiveLoader.Load(RegistryFilePath, out var logs);
                AppliedLogs = logs;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[OfflineRegistryReader] Hive non leggibile {RegistryFilePath}: {ex.Message}");
                return retList;
            }

            string userOfHive = FindOfflineUsername(hive);

            foreach (string location in Parser.GetRegistryLocations())
            {
                try
                {
                    foreach (RegistryKeyWrapper keyWrapper in IterateRegistry(hive.GetKey(location), hive, null))
                    {
                        keyWrapper.RegistryUser = userOfHive;
                        retList.Add(keyWrapper);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[OfflineRegistryReader] Unable to retrieve keys in {RegistryFilePath} at {location}: {ex.Message}");
                }
            }

            return retList;
        }

        /// <summary>
        /// Utente proprietario dell'hive: nome della cartella del profilo (…\Users\&lt;nome&gt;\NTUSER.DAT o
        /// …\Users\&lt;nome&gt;\AppData\Local\Microsoft\Windows\UsrClass.dat). Vale per entrambi gli hive.
        /// </summary>
        private string FindOfflineUsername(RegistryHive hive)
        {
            var parts = Path.GetFullPath(RegistryFilePath).Split('\\');
            int users = Array.FindLastIndex(parts, p => p.Equals("Users", StringComparison.OrdinalIgnoreCase)
                                                     || p.Equals("Documents and Settings", StringComparison.OrdinalIgnoreCase));
            if (users >= 0 && users + 1 < parts.Length - 1)
                return parts[users + 1];

            return string.Empty;
        }

        /// <summary>
        /// Visita ricorsiva di BagMRU: per ogni sottochiave numerica N il valore N della chiave corrente contiene lo shell item.
        /// </summary>
        static List<RegistryKeyWrapper> IterateRegistry(RegistryKey rk, RegistryHive hive, RegistryKeyWrapper parent)
        {
            List<RegistryKeyWrapper> retList = new List<RegistryKeyWrapper>();
            if (rk == null)
            {
                return retList;
            }

            var mruFirst = RegistryKeyWrapper.MostRecentIndex(
                rk.Values.FirstOrDefault(v => v.ValueName.Equals("MRUListEx", StringComparison.OrdinalIgnoreCase))?.ValueDataRaw);
            var rkLastWrite = rk.LastWriteTime?.LocalDateTime;

            foreach (RegistryKey subKey in rk.SubKeys)
            {
                if (!int.TryParse(subKey.KeyName, out int index))
                {
                    continue;
                }

                RegistryKeyWrapper rkNextWrapper = null;
                try
                {
                    KeyValue rkValue = rk.Values.FirstOrDefault(val => val.ValueName == subKey.KeyName);
                    if (rkValue != null)
                    {
                        rkNextWrapper = new RegistryKeyWrapper(subKey, rkValue.ValueDataRaw, hive, parent);
                        if (mruFirst == index)
                            rkNextWrapper.LastInteracted = rkLastWrite;
                        retList.Add(rkNextWrapper);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[OfflineRegistryReader] {subKey.KeyPath}: {ex.Message}");
                }

                retList.AddRange(IterateRegistry(subKey, hive, rkNextWrapper));
            }

            return retList;
        }
    }
}
