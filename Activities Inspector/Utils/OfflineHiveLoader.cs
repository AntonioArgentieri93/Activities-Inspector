using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Activities_Inspector.Utils
{
    /// <summary>
    /// Caricamento di hive offline con applicazione dei transaction log (.LOG1/.LOG2).
    /// Un hive copiato da un sistema acceso è spesso "sporco" (PrimarySequenceNumber != SecondarySequenceNumber):
    /// le modifiche più recenti stanno solo nei log e senza applicarli si perdono (come fanno RECmd/SBECmd).
    /// </summary>
    public static class OfflineHiveLoader
    {
        public static global::Registry.RegistryHive Load(string hivePath, out IReadOnlyList<string> appliedLogs)
            => Load(File.ReadAllBytes(hivePath), hivePath, out appliedLogs);

        public static global::Registry.RegistryHive Load(byte[] hiveBytes, string hivePath, out IReadOnlyList<string> appliedLogs)
        {
            appliedLogs = Array.Empty<string>();
            var hive = new global::Registry.RegistryHive(hiveBytes, hivePath);

            if (IsDirty(hive))
            {
                var logs = FindTransactionLogs(hivePath);
                if (logs.Count > 0)
                {
                    try
                    {
                        hive.ProcessTransactionLogs(logs, true);
                        appliedLogs = logs;
                    }
                    catch (Exception ex)
                    {
                        // Log non validi/non pertinenti: si prosegue con l'hive così com'è (stato pulito)
                        System.Diagnostics.Debug.WriteLine($"[OfflineHiveLoader] Log non applicati per {hivePath}: {ex.Message}");
                        hive = new global::Registry.RegistryHive(hiveBytes, hivePath);
                    }
                }
            }

            hive.ParseHive();
            return hive;
        }

        internal static bool IsDirty(global::Registry.RegistryHive hive)
            => hive.Header.PrimarySequenceNumber != hive.Header.SecondarySequenceNumber;

        /// <summary>File &lt;hive&gt;.LOG, .LOG1, .LOG2 accanto all'hive (stessa cartella).</summary>
        internal static List<string> FindTransactionLogs(string hivePath)
        {
            var dir = Path.GetDirectoryName(hivePath);
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return new List<string>();

            var baseName = Path.GetFileName(hivePath);
            return Directory.EnumerateFiles(dir, baseName + ".LOG*")
                .Where(f =>
                {
                    var ext = Path.GetExtension(f).ToUpperInvariant();
                    return ext == ".LOG" || ext == ".LOG1" || ext == ".LOG2";
                })
                .Where(f => new FileInfo(f).Length > 0)
                .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }
}
