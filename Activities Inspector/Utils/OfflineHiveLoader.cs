using Registry;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Activities_Inspector.Utils
{
    /// <summary>
    /// Caricamento di hive con applicazione dei transaction log (.LOG1/.LOG2).
    /// Un hive letto mentre il sistema è acceso (o copiato da un sistema acceso) è spesso "sporco"
    /// (PrimarySequenceNumber != SecondarySequenceNumber): le modifiche più recenti stanno solo nei log e senza
    /// applicarli si perdono (es. un dispositivo USB ricollegato pochi minuti prima). Come fanno RECmd/SBECmd.
    /// </summary>
    public static class OfflineHiveLoader
    {
        /// <summary>Carica un hive da file; i log sono cercati accanto all'hive.</summary>
        public static RegistryHive Load(string hivePath, out IReadOnlyList<string> appliedLogs)
            => Load(File.ReadAllBytes(hivePath), hivePath, out appliedLogs);

        /// <summary>Carica un hive già letto in memoria; i log sono cercati accanto a <paramref name="hivePath"/>.</summary>
        public static RegistryHive Load(byte[] hiveBytes, string hivePath, out IReadOnlyList<string> appliedLogs)
        {
            var logFiles = FindTransactionLogs(hivePath);
            var logs = logFiles.Select(f => new TransactionLogFileInfo(f, File.ReadAllBytes(f))).ToList();

            var hive = Load(hiveBytes, hivePath, logs, out var applied);
            appliedLogs = applied ? logFiles : (IReadOnlyList<string>)Array.Empty<string>();
            return hive;
        }

        /// <summary>
        /// Carica un hive e, se è sporco, ne applica i log forniti. Ritorna sempre un hive già analizzato
        /// (ParseHive eseguito una sola volta: una seconda chiamata genera "ParseHive already called").
        /// Se i log non sono applicabili si ripiega sull'hive così com'è.
        /// </summary>
        public static RegistryHive Load(byte[] hiveBytes, string hivePath, IReadOnlyList<TransactionLogFileInfo> logs,
            out bool logsApplied)
        {
            logsApplied = false;
            var hive = new RegistryHive(hiveBytes, hivePath);

            if (logs != null && logs.Count > 0 && IsDirty(hive))
            {
                try
                {
                    // Con updateExistingData=false l'istanza non viene modificata né analizzata: ritorna solo i byte aggiornati
                    var patchedBytes = hive.ProcessTransactionLogs(logs.ToList(), false);
                    if (patchedBytes != null && patchedBytes.Length > 0)
                    {
                        hive = new RegistryHive(patchedBytes, hivePath);
                        logsApplied = true;
                    }
                }
                catch (Exception ex)
                {
                    // Log non validi/non pertinenti: si prosegue con l'hive così com'è (stato dell'ultimo checkpoint)
                    System.Diagnostics.Debug.WriteLine($"[OfflineHiveLoader] Log non applicati per {hivePath}: {ex.Message}");
                    hive = new RegistryHive(hiveBytes, hivePath);
                    logsApplied = false;
                }
            }

            hive.ParseHive();
            return hive;
        }

        internal static bool IsDirty(RegistryHive hive)
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
