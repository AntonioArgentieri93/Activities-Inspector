using CSharpFunctionalExtensions;
using Activities_Inspector.Constants;
using Activities_Inspector.Models;
using Activities_Inspector.Utils;
using Microsoft.Win32;
using Registry;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Activities_Inspector.Services
{
    public class RecentFilesService : IRecentFilesService
    {
        private readonly Evidence.IEvidenceSourceProvider _sources;
        private static bool _beefResidualDiagnosticsEnabled;

        public RecentFilesService(Evidence.IEvidenceSourceProvider sources)
        {
            _sources = sources;
        }

        public int SkippedFilesCount { get; private set; }

        public IReadOnlyList<IntegrityRecord> LastIntegrityManifest { get; private set; }
            = new List<IntegrityRecord>();

        public async Task<Result<List<RecentFolderEntry>>> GetRecentFilesAsync(CancellationToken cancellationToken = default)
        {
            SkippedFilesCount = 0;
            var manifest = new List<IntegrityRecord>();
            LastIntegrityManifest = manifest;

            try
            {
                var allEntries = new List<RecentFolderEntry>();

                // ============================================
                // 1. RECENT FOLDER (.lnk files)
                // ============================================
                var recentDirs = _sources.Current.GetRecentDirectories()
                    .Where(Directory.Exists)
                    .ToList();

                System.Diagnostics.Debug.WriteLine($"[RecentFilesService] Found {recentDirs.Count} recent directories: {string.Join(", ", recentDirs)}");

                var lnkEntries = new List<RecentFolderEntry>();
                if (recentDirs.Count > 0)
                {
                    var orderedFiles = recentDirs
                        .SelectMany(d => new DirectoryInfo(d).GetFiles(AppConstants.Paths.RecentExtension))
                        .OrderBy(f => f.LastWriteTime)
                        .ToList();

                    System.Diagnostics.Debug.WriteLine($"[RecentFilesService] Found {orderedFiles.Count} .lnk files in Recent folder");

                    lnkEntries = await Task.Run(() =>
                    {
                        var list = new List<RecentFolderEntry>();
                        int parsedCount = 0;
                        int skippedNoPath = 0;
                        int skippedParse = 0;

                        foreach (var file in orderedFiles)
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            LnkFile lnkFile = null;

                            byte[] raw;
                            try
                            {
                                raw = File.ReadAllBytes(file.FullName);
                            }
                            catch (Exception ex)
                            {
                                SkippedFilesCount++;
                                System.Diagnostics.Debug.WriteLine($"[RecentFilesService] Failed to read {file.FullName}: {ex.Message}");
                                manifest.Add(IntegrityHasher.HashFile(file.FullName, EntryType.Recents));
                                continue;
                            }

                            manifest.Add(IntegrityHasher.HashBytes(raw, file.FullName, EntryType.Recents));

                            try
                            {
                                lnkFile = LoadFile(raw, file.FullName);
                            }
                            catch (Exception ex)
                            {
                                SkippedFilesCount++;
                                skippedParse++;
                                System.Diagnostics.Debug.WriteLine($"[RecentFilesService] Failed to parse LNK {file.FullName}: {ex.Message}");
                                continue;
                            }

                            if (lnkFile == null)
                            {
                                skippedParse++;
                                continue;
                            }

                            // LOG: LNK parser output
                            System.Diagnostics.Debug.WriteLine($"[RecentFilesService] LNK {file.Name}: LocalPath='{lnkFile.LocalPath}', CommonPath='{lnkFile.CommonPath}', NetworkShare='{lnkFile.NetworkShareInfo?.NetworkShareName}', TargetIDs.Count={lnkFile.TargetIDs?.Count}, ExtraBlocks.Count={lnkFile.ExtraBlocks?.Count}");

                            var fullPath = ResolveTargetPath(
                                lnkFile.LocalPath,
                                lnkFile.NetworkShareInfo?.NetworkShareName,
                                lnkFile.CommonPath,
                                lnkFile.TargetIDs,
                                lnkFile.ExtraBlocks);

                            if (string.IsNullOrEmpty(fullPath))
                            {
                                skippedNoPath++;
                                System.Diagnostics.Debug.WriteLine($"[RecentFilesService] LNK {file.Name}: Could not resolve path, skipping");
                                continue;
                            }

                            parsedCount++;
                            var actionTime = file.LastWriteTime;
                            var fileName = Path.GetFileNameWithoutExtension(file.Name);
                            var dataSource = file.FullName;

                            var entry = new RecentFolderEntry(actionTime, fileName, dataSource, fullPath)
                            {
                                SkippedShellItems = lnkFile.SkippedShellItems
                            };
                            list.Add(entry);
                        }

                        System.Diagnostics.Debug.WriteLine($"[RecentFilesService] LNK Summary: Parsed={parsedCount}, SkippedNoPath={skippedNoPath}, SkippedParse={skippedParse}");
                        return list;
                    }, cancellationToken);
                }

                System.Diagnostics.Debug.WriteLine($"[RecentFilesService] LNK Entries: {lnkEntries.Count}");
                allEntries.AddRange(lnkEntries);

                // ============================================
                // 2. REGISTRY (RecentDocs + OpenSaveMRU)
                // ============================================
                if (_sources.Current.IsLive)
                {
                    System.Diagnostics.Debug.WriteLine($"[RecentFilesService] Live mode: Reading registry via API");
                    var regEntries = await GetRecentFromRegistryLiveAsync(manifest, cancellationToken);
                    MergeRecentDocsIntoLnk(regEntries, lnkEntries);
                    System.Diagnostics.Debug.WriteLine($"[RecentFilesService] Registry Live Entries: {regEntries.Count}");
                    allEntries.AddRange(regEntries);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[RecentFilesService] Offline mode: Reading NTUSER.DAT hive");
                    var regEntries = await GetRecentFromRegistryAsync(manifest, cancellationToken);
                    MergeRecentDocsIntoLnk(regEntries, lnkEntries);
                    System.Diagnostics.Debug.WriteLine($"[RecentFilesService] Registry Offline Entries: {regEntries.Count}");
                    allEntries.AddRange(regEntries);
                }

                // Nessuna unione per percorso (come NirSoft RecentFilesView): ogni .lnk e ogni valore MRU
                // è un artefatto distinto con la propria sorgente. Solo le voci RecentDocs che puntano a un
                // .lnk già elencato sono unite (MergeRecentDocsIntoLnk): sono lo stesso artefatto.
                var deduped = allEntries;

                // Check if any source was actually available
                bool hasRecentDirs = recentDirs.Count > 0;
                bool hasRegistryHives = false;
                if (!_sources.Current.IsLive)
                {
                    var hivePaths = _sources.Current.GetUserHivePaths("NTUSER.DAT").ToList();
                    hasRegistryHives = hivePaths.Count > 0;
                }

                if (!hasRecentDirs && !hasRegistryHives && deduped.Count == 0)
                    return Result.Failure<List<RecentFolderEntry>>("Nessuna sorgente file recenti leggibile");

                System.Diagnostics.Debug.WriteLine($"[RecentFilesService] FINAL: Total unique entries = {deduped.Count} (before dedupe: {allEntries.Count})");
                return Result.Success(deduped);
            }
            catch (Exception ex) when (!(ex is OperationCanceledException))
            {
                System.Diagnostics.Debug.WriteLine($"[RecentFilesService] ERROR: {ex}");
                return Result.Failure<List<RecentFolderEntry>>(ex.ToString());
            }
        }

        internal static string ResolveTargetPath(string localPath, string networkShareName, string commonPath,
            List<ExtensionBlocks.ShellBag> targetIDs = null, List<Activities_Inspector.ExtraData.ExtraData.ExtraDataBase> extraBlocks = null)
        {
            System.Diagnostics.Debug.WriteLine($"[ResolveTargetPath] Input: LocalPath='{localPath}', NetworkShare='{networkShareName}', CommonPath='{commonPath}', TargetIDs={targetIDs?.Count}, ExtraBlocks={extraBlocks?.Count}");

            // 1. LinkInfo locale (MS-SHLLINK): destinazione = LocalBasePath + CommonPathSuffix.
            //    Se il file sta in una cartella condivisa, LocalBasePath è la radice locale della condivisione
            //    (es. "C:\Users\") e il suffisso il resto ("anton\Downloads\x.png"): è il percorso che usa Windows
            //    (e NirSoft), preferito alla forma di rete \\host\share\...
            if (!string.IsNullOrEmpty(localPath))
            {
                var suffix = (commonPath ?? string.Empty).TrimStart('\\');
                if (suffix.Length > 0 && localPath.EndsWith("\\"))
                {
                    var combined = localPath + suffix;
                    System.Diagnostics.Debug.WriteLine($"[ResolveTargetPath] Using LocalBasePath+CommonPathSuffix: {combined}");
                    return combined;
                }

                System.Diagnostics.Debug.WriteLine($"[ResolveTargetPath] Using LocalPath: {localPath}");
                return localPath;
            }

            // 2. Prova network share + common path
            if (!string.IsNullOrEmpty(networkShareName))
            {
                var share = networkShareName.TrimEnd('\\');
                var suffix = (commonPath ?? string.Empty).TrimStart('\\');
                if (suffix.Length > 0)
                {
                    var result = share + "\\" + suffix;
                    System.Diagnostics.Debug.WriteLine($"[ResolveTargetPath] Using NetworkShare+Suffix: {result}");
                    return result;
                }
                if (share.Length > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"[ResolveTargetPath] Using NetworkShare only: {share}");
                    return share;
                }
            }

            // 3. Prova CommonPath da solo
            if (!string.IsNullOrEmpty(commonPath))
            {
                System.Diagnostics.Debug.WriteLine($"[ResolveTargetPath] Using CommonPath: {commonPath}");
                return commonPath;
            }

            // 4. Prova ExtraBlocks (es. EnvironmentVariableDataBlock contiene path espanso)
            if (extraBlocks != null && extraBlocks.Count > 0)
            {
                var extraPath = ExtractPathFromExtraBlocks(extraBlocks);
                if (!string.IsNullOrEmpty(extraPath))
                {
                    System.Diagnostics.Debug.WriteLine($"[ResolveTargetPath] Using ExtraBlocks: {extraPath}");
                    return extraPath;
                }
            }

            // 5. Fallback: ricostruisci da ShellItems (TargetIDs)
            if (targetIDs != null && targetIDs.Count > 0)
            {
                var shellPath = BuildPathFromShellItems(targetIDs);
                if (!string.IsNullOrEmpty(shellPath))
                {
                    System.Diagnostics.Debug.WriteLine($"[ResolveTargetPath] Using ShellItems: {shellPath}");
                    return shellPath;
                }

                // If that fails, try to extract ANY path-like string from ALL shell items
                foreach (var item in targetIDs)
                {
                    try
                    {
                        var props = new[] { "Value", "FriendlyName", "ShortName", "AbsolutePath", "Path", "Name", "DisplayName" };
                        foreach (var propName in props)
                        {
                            var prop = item.GetType().GetProperty(propName);
                            if (prop != null)
                            {
                                var val = prop.GetValue(item) as string;
                                if (!string.IsNullOrEmpty(val) && IsValidPath(val))
                                {
                                    System.Diagnostics.Debug.WriteLine($"[ResolveTargetPath] Using ShellItem.{propName}: {val}");
                                    return val;
                                }
                            }
                        }
                    }
                    catch { }
                }
            }

            System.Diagnostics.Debug.WriteLine($"[ResolveTargetPath] FAILED - no path found");
            return string.Empty;
        }

        private static string ExtractPathFromExtraBlocks(List<Activities_Inspector.ExtraData.ExtraData.ExtraDataBase> extraBlocks)
        {
            foreach (var block in extraBlocks)
            {
                try
                {
                    var typeName = block.GetType().Name;
                    System.Diagnostics.Debug.WriteLine($"[ExtractPathFromExtraBlocks] Block: {typeName}");

                    // EnvironmentVariableDataBlock (signature 0xA0000001) contains expanded path
                    if (typeName.Contains("Environment") || typeName.Contains("Env"))
                    {
                        var props = block.GetType().GetProperties();
                        foreach (var prop in props)
                        {
                            var val = prop.GetValue(block) as string;
                            if (!string.IsNullOrEmpty(val) && (val.Contains(":") || val.Contains("\\")))
                            {
                                System.Diagnostics.Debug.WriteLine($"[ExtractPathFromExtraBlocks] Found path in {typeName}.{prop.Name}: {val}");
                                return val;
                            }
                        }
                    }
                    // ConsoleDataBlock (signature 0xA0000002) might have working dir
                    if (typeName.Contains("Console"))
                    {
                        var props = block.GetType().GetProperties();
                        foreach (var prop in props)
                        {
                            var val = prop.GetValue(block) as string;
                            if (!string.IsNullOrEmpty(val) && (val.Contains(":") || val.Contains("\\")))
                            {
                                System.Diagnostics.Debug.WriteLine($"[ExtractPathFromExtraBlocks] Found path in {typeName}.{prop.Name}: {val}");
                                return val;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[ExtractPathFromExtraBlocks] Error: {ex.Message}");
                }
            }
            return string.Empty;
        }

        private static bool IsValidPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            var trimmed = path.Trim();
            // Must look like a Windows path: C:\..., \\server\..., or at least contain \ and :
            return (trimmed.Length >= 3 && trimmed[1] == ':' && trimmed[2] == '\\') ||
                   trimmed.StartsWith("\\\\") ||
                   (trimmed.Contains(":") && trimmed.Contains("\\") && trimmed.Length > 5);
        }

        private static string BuildPathFromShellItems(List<ExtensionBlocks.ShellBag> shellItems)
        {
            var pathParts = new List<string>();

            foreach (var item in shellItems)
            {
                try
                {
                    // Evita item.ToString() che può lanciare NullReferenceException
                    // Accede direttamente alle proprietà note
                    string itemStr = null;

                    // Prova proprietà comuni di ExtensionBlocks.ShellBag
                    var props = new[] { "Value", "FriendlyName", "ShortName", "AbsolutePath", "Path", "Name", "DisplayName" };
                    foreach (var propName in props)
                    {
                        var prop = item.GetType().GetProperty(propName);
                        if (prop != null)
                        {
                            var val = prop.GetValue(item) as string;
                            if (!string.IsNullOrEmpty(val))
                            {
                                itemStr = val;
                                break;
                            }
                        }
                    }

                    if (string.IsNullOrEmpty(itemStr)) continue;

                    // Estrae il nome (formato tipico: "Name: xxx" o percorso)
                    var nameMatch = System.Text.RegularExpressions.Regex.Match(itemStr, @"Name:\s*([^\r\n]+)");
                    if (nameMatch.Success)
                    {
                        pathParts.Add(nameMatch.Groups[1].Value.Trim());
                    }
                    else
                    {
                        // Prova a usare la stringa se sembra un percorso
                        if (itemStr.Contains("\\") || itemStr.Contains(":"))
                        {
                            pathParts.Add(itemStr.Trim());
                        }
                    }
                }
                catch
                {
                    // Ignora item problematici
                }
            }

            if (pathParts.Count > 0)
            {
                // Ricostruisce il percorso (ultimo elemento potrebbe essere il file)
                return string.Join("\\", pathParts.Where(p => !string.IsNullOrEmpty(p)));
            }
            return string.Empty;
        }

        private async Task<List<RecentFolderEntry>> GetRecentFromRegistryAsync(List<IntegrityRecord> manifest, CancellationToken cancellationToken)
        {
            var entries = new List<RecentFolderEntry>();
            var hivePaths = _sources.Current.GetUserHivePaths("NTUSER.DAT").ToList();

            System.Diagnostics.Debug.WriteLine($"[GetRecentFromRegistryAsync] Found {hivePaths.Count} NTUSER.DAT hives");

            foreach (var hivePath in hivePaths)
            {
                cancellationToken.ThrowIfCancellationRequested();

                byte[] hiveBytes;
                try
                {
                    hiveBytes = File.ReadAllBytes(hivePath);
                }
                catch
                {
                    manifest.Add(IntegrityHasher.HashFile(hivePath, EntryType.Recents));
                    continue;
                }

                manifest.Add(IntegrityHasher.HashBytes(hiveBytes, hivePath, EntryType.Recents));

                try
                {
                    var hive = new Registry.RegistryHive(hiveBytes, hivePath);
                    hive.ParseHive();

                    // Read from RecentDocs
                    var recentDocsEntries = ParseRecentDocsKey(hive, hivePath, manifest, cancellationToken);
                    System.Diagnostics.Debug.WriteLine($"[GetRecentFromRegistryAsync] RecentDocs entries from {hivePath}: {recentDocsEntries.Count}");
                    entries.AddRange(recentDocsEntries);

                    // Read from OpenSaveMRU
                    var openSaveMruEntries = ParseOpenSaveMRUKey(hive, hivePath, manifest, cancellationToken);
                    System.Diagnostics.Debug.WriteLine($"[GetRecentFromRegistryAsync] OpenSaveMRU entries from {hivePath}: {openSaveMruEntries.Count}");
                    entries.AddRange(openSaveMruEntries);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[GetRecentFromRegistryAsync] Error parsing hive {hivePath}: {ex.Message}");
                }
            }

            return entries;
        }

        private static List<RecentFolderEntry> ParseRecentDocsKey(Registry.RegistryHive hive, string hivePath, List<IntegrityRecord> manifest, CancellationToken cancellationToken)
        {
            var entries = new List<RecentFolderEntry>();
            var recentDocsKey = GetKeyInsensitive(hive, AppConstants.Registry.RecentDocsPath);
            if (recentDocsKey == null)
            {
                System.Diagnostics.Debug.WriteLine($"[ParseRecentDocsKey] RecentDocs key not found in hive");
                return entries;
            }

            foreach (var extKey in recentDocsKey.SubKeys)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var mruListEx = GetRawValue(extKey, "MRUListEx");
                if (mruListEx == null || mruListEx.Length == 0)
                {
                    continue;
                }

                for (int i = 0; i < mruListEx.Length / 4; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var index = BitConverter.ToInt32(mruListEx, i * 4);
                    if (index == -1 || index == 0xFFFFFFFF) continue;

                    var valueName = index.ToString();
                    var valueObj = GetRawValue(extKey, valueName);
                    if (valueObj == null) continue;

                    string path = null;
                    if (valueObj is byte[] valueBytes)
                    {
                        // Stesso formato del percorso live: nome UTF-16LE all'inizio del valore (non un ID list)
                        path = ParseRecentDocsValue(valueBytes);
                    }
                    else
                    {
                        path = valueObj.ToString();
                    }

                    if (string.IsNullOrEmpty(path)) continue;

                    // Come NirSoft: solo la voce più recente (MRU[0]) ha data certa = ultima modifica della chiave
                    DateTime? actionTime = i == 0 ? extKey.LastWriteTime?.LocalDateTime : null;
                    var fileName = Path.GetFileName(path);
                    var dataSource = $"{hivePath}\\{AppConstants.Registry.RecentDocsPath}\\{extKey.KeyName}";

                    var entry = new RecentFolderEntry(actionTime, fileName, dataSource, path)
                    {
                        SkippedShellItems = 0,
                        LinkedLnkName = ExtractRecentDocsLnkName(valueObj as byte[])
                    };
                    entries.Add(entry);
                }
            }

            return entries;
        }

        private static List<RecentFolderEntry> ParseOpenSaveMRUKey(Registry.RegistryHive hive, string hivePath, List<IntegrityRecord> manifest, CancellationToken cancellationToken)
        {
            var entries = new List<RecentFolderEntry>();
            var openSaveMruKey = GetKeyInsensitive(hive, AppConstants.Registry.OpenSavePidlMRUPath);
            if (openSaveMruKey == null)
            {
                System.Diagnostics.Debug.WriteLine($"[ParseOpenSaveMRUKey] OpenSaveMRU key not found");
                return entries;
            }

            System.Diagnostics.Debug.WriteLine($"[ParseOpenSaveMRUKey] Found {openSaveMruKey.SubKeys.Count} extension subkeys");
            var shellFolders = ReadShellFolders(hive);

            foreach (var extKey in openSaveMruKey.SubKeys)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var mruListEx = GetRawValue(extKey, "MRUListEx");
                if (mruListEx == null || mruListEx.Length == 0)
                {
                    System.Diagnostics.Debug.WriteLine($"[ParseOpenSaveMRUKey] No MRUListEx in {extKey.KeyName}");
                    continue;
                }

                System.Diagnostics.Debug.WriteLine($"[ParseOpenSaveMRUKey] {extKey.KeyName}: MRUListEx length={mruListEx.Length}");

                for (int i = 0; i < mruListEx.Length / 4; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var index = BitConverter.ToInt32(mruListEx, i * 4);
                    if (index == -1 || index == 0xFFFFFFFF) continue;

                    var valueName = index.ToString();
                    var valueObj = GetRawValue(extKey, valueName);
                    if (valueObj == null) continue;

                    string path = null;
                    if (valueObj is byte[] valueBytes)
                    {
                        _beefResidualDiagnosticsEnabled = true;
                        try { path = ParseShellItemPath(valueBytes, shellFolders); }
                        finally { _beefResidualDiagnosticsEnabled = false; }
                    }
                    else
                    {
                        path = valueObj.ToString();
                    }

                    System.Diagnostics.Debug.WriteLine($"[ParseOpenSaveMRUKey] {extKey.KeyName}[{valueName}]: raw='{valueObj}', parsed='{path}'");

                    if (string.IsNullOrEmpty(path)) continue;

                    // Come NirSoft: solo la voce più recente (MRU[0]) ha data certa = ultima modifica della chiave
                    DateTime? actionTime = i == 0 ? extKey.LastWriteTime?.LocalDateTime : null;
                    var fileName = Path.GetFileName(path);
                    var dataSource = $"{hivePath}\\{AppConstants.Registry.OpenSavePidlMRUPath}\\{extKey.KeyName}";

                    var entry = new RecentFolderEntry(actionTime, fileName, dataSource, path)
                    {
                        SkippedShellItems = 0
                    };
                    entries.Add(entry);
                }
            }

            return entries;
        }

        private async Task<List<RecentFolderEntry>> GetRecentFromRegistryLiveAsync(List<IntegrityRecord> manifest, CancellationToken cancellationToken)
        {
            var entries = new List<RecentFolderEntry>();

            try
            {
                // Read from RecentDocs (HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\RecentDocs)
                using (var recentDocsKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(AppConstants.Registry.RecentDocsPath))
                {
                    if (recentDocsKey != null)
                    {
                        manifest.Add(IntegrityRecord.LiveSource(EntryType.Recents,
                            $"HKCU\\{AppConstants.Registry.RecentDocsPath} (registry live)"));

                        foreach (var extKeyName in recentDocsKey.GetSubKeyNames())
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            using (var extKey = recentDocsKey.OpenSubKey(extKeyName))
                            {
                                if (extKey == null) continue;

                                var mruListEx = extKey.GetValue("MRUListEx") as byte[];
                                if (mruListEx == null || mruListEx.Length == 0)
                                {
                                    continue;
                                }

                                for (int i = 0; i < mruListEx.Length / 4; i++)
                                {
                                    cancellationToken.ThrowIfCancellationRequested();

                                    var index = BitConverter.ToInt32(mruListEx, i * 4);
                                    if (index == -1 || index == 0xFFFFFFFF) continue;

                                    var valueName = index.ToString();
                                    var valueObj = extKey.GetValue(valueName);
                                    if (valueObj == null) continue;

                                    string path = null;
                                    if (valueObj is byte[] valueBytes)
                                    {
                                        // RecentDocs values contain simple UTF-16LE filenames, not full PIDLs
                                        path = ParseRecentDocsValue(valueBytes);
                                    }
                                    else
                                    {
                                        path = valueObj.ToString();
                                    }

                                    if (string.IsNullOrEmpty(path)) continue;

                                    // Come NirSoft: solo la voce più recente (MRU[0]) ha data certa = ultima modifica della chiave
                                    DateTime? actionTime = i == 0 ? GetKeyLastWriteTime(extKey) : null;
                                    var fileName = Path.GetFileName(path);
                                    var dataSource = $"HKCU\\{AppConstants.Registry.RecentDocsPath}\\{extKeyName}";

                                    var entry = new RecentFolderEntry(actionTime, fileName, dataSource, path)
                                    {
                                        SkippedShellItems = 0,
                        LinkedLnkName = ExtractRecentDocsLnkName(valueObj as byte[])
                                    };
                                    entries.Add(entry);
                                }
                            }
                        }
                    }
                }

                // Read from OpenSaveMRU (HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\ComDlg32\OpenSaveMRU)
                var shellFolders = ReadShellFoldersLive();
                using (var openSaveMruKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(AppConstants.Registry.OpenSavePidlMRUPath))
                {
                    if (openSaveMruKey != null)
                    {
                        manifest.Add(IntegrityRecord.LiveSource(EntryType.Recents,
                            $"HKCU\\{AppConstants.Registry.OpenSavePidlMRUPath} (registry live)"));

                        foreach (var extKeyName in openSaveMruKey.GetSubKeyNames())
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            using (var extKey = openSaveMruKey.OpenSubKey(extKeyName))
                            {
                                if (extKey == null) continue;

                                var mruListEx = extKey.GetValue("MRUListEx") as byte[];
                                if (mruListEx == null || mruListEx.Length == 0)
                                {
                                    continue;
                                }

                                for (int i = 0; i < mruListEx.Length / 4; i++)
                                {
                                    cancellationToken.ThrowIfCancellationRequested();

                                    var index = BitConverter.ToInt32(mruListEx, i * 4);
                                    if (index == -1 || index == 0xFFFFFFFF) continue;

                                    var valueName = index.ToString();
                                    var valueObj = extKey.GetValue(valueName);
                                    if (valueObj == null) continue;

string path = null;
                    if (valueObj is byte[] valueBytes)
                    {
                        // OpenSavePidlMRU contains full PIDLs, parse with shell item parser
                        _beefResidualDiagnosticsEnabled = true;
                        try { path = ParseShellItemPath(valueBytes, shellFolders); }
                        finally { _beefResidualDiagnosticsEnabled = false; }
                    }
                    else
                    {
                        path = valueObj.ToString();
                    }

                                    System.Diagnostics.Debug.WriteLine($"[Live][OpenSavePidlMRU] {extKeyName}[{valueName}]: parsed='{path}'");

                                    if (string.IsNullOrEmpty(path)) continue;

                                    // Come NirSoft: solo la voce più recente (MRU[0]) ha data certa = ultima modifica della chiave
                                    DateTime? actionTime = i == 0 ? GetKeyLastWriteTime(extKey) : null;
                                    var fileName = Path.GetFileName(path);
                                    var dataSource = $"HKCU\\{AppConstants.Registry.OpenSavePidlMRUPath}\\{extKeyName}";

                                    var entry = new RecentFolderEntry(actionTime, fileName, dataSource, path)
                                    {
                                        SkippedShellItems = 0
                                    };
                                    entries.Add(entry);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[GetRecentFromRegistryLiveAsync] Error: {ex}");
            }

            System.Diagnostics.Debug.WriteLine($"[GetRecentFromRegistryLiveAsync] Total entries: {entries.Count}");
            return entries;
        }

        /// <summary>
        /// Valore RecentDocs = [nome UTF-16LE\0][shell item del .lnk in Recent\ ...]: ritorna il nome del .lnk.
        /// </summary>
        internal static string ExtractRecentDocsLnkName(byte[] data)
        {
            if (data == null) return null;
            for (int i = 0; i + 1 < data.Length; i += 2)
            {
                if (data[i] != 0 || data[i + 1] != 0) continue;
                int start = i + 2;
                if (start + 4 > data.Length) return null;
                var rest = new byte[data.Length - start];
                Buffer.BlockCopy(data, start, rest, 0, rest.Length);
                var lnk = ParseShellItemPath(rest);
                if (string.IsNullOrEmpty(lnk)) return null;
                lnk = Path.GetFileName(lnk);
                return lnk.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase) ? lnk : null;
            }
            return null;
        }

        /// <summary>
        /// Rimuove le voci RecentDocs che puntano a un .lnk già presente tra le voci della cartella Recent:
        /// sono lo stesso evento, e la voce .lnk ha percorso completo e data precisa.
        /// Le voci RecentDocs senza .lnk corrispondente (es. .lnk ruotato/eliminato) restano.
        /// </summary>
        internal static void MergeRecentDocsIntoLnk(List<RecentFolderEntry> registryEntries, IEnumerable<RecentFolderEntry> lnkEntries)
        {
            var lnkNames = new HashSet<string>(
                lnkEntries.Select(e => Path.GetFileName(e.DataSource)), StringComparer.OrdinalIgnoreCase);
            int removed = registryEntries.RemoveAll(e => e.LinkedLnkName != null && lnkNames.Contains(e.LinkedLnkName));
            System.Diagnostics.Debug.WriteLine($"[RecentFilesService] RecentDocs merged into .lnk entries: {removed}");
        }

        private static string ParseRecentDocsValue(byte[] data)
        {
            if (data == null || data.Length < 2) return null;

            try
            {
                // RecentDocs values contain the filename as a null-terminated UTF-16LE string at the start
                // Format: [filename in UTF-16LE, null-terminated][optional additional data]
                
                // Find the first null-terminated UTF-16LE string
                for (int i = 0; i < data.Length - 3; i += 2)
                {
                    if (data[i] == 0 && data[i + 1] == 0)
                    {
                        // Found null terminator - extract string from start to here
                        if (i > 0)
                        {
                            var nameBytes = new byte[i];
                            Buffer.BlockCopy(data, 0, nameBytes, 0, i);
                            var name = Encoding.Unicode.GetString(nameBytes);
                            // Posizione certa per formato (offset 0): ammette qualsiasi alfabeto
                            if (!string.IsNullOrEmpty(name) && IsPlausibleFileName(name))
                            {
                                System.Diagnostics.Debug.WriteLine($"[ParseRecentDocsValue] Extracted filename: '{name}'");
                                return name;
                            }
                        }
                        break;
                    }
                }

                // Fallback: try to decode entire data as UTF-16LE and take first null-terminated part
                var fullStr = Encoding.Unicode.GetString(data);
                var nullIndex = fullStr.IndexOf('\0');
                if (nullIndex > 0)
                {
                    var name = fullStr.Substring(0, nullIndex);
                    if (!string.IsNullOrEmpty(name) && IsValidPathComponent(name))
                    {
                        System.Diagnostics.Debug.WriteLine($"[ParseRecentDocsValue] Extracted filename (fallback): '{name}'");
                        return name;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ParseRecentDocsValue] Exception: {ex.Message}");
            }
            return null;
        }

        /// <summary>
        /// Root folder (0x1F) delle cartelle note → nome del valore in Explorer\Shell Folders + nome di fallback.
        /// Include i CLSID "Questo PC\..." (Win10/11) e quelli legacy (Win7/8). Questo PC non ha nome proprio:
        /// il percorso prosegue con l'item volume (C:\).
        /// </summary>
        private static readonly Dictionary<Guid, (string ShellFolderValue, string DisplayName)> KnownFolderRoots =
            new Dictionary<Guid, (string, string)>
            {
                [new Guid("b4bfcc3a-db2c-424c-b029-7fe99a87c641")] = ("Desktop", "Desktop"),
                [new Guid("d3162b92-9365-467a-956b-92703aca08af")] = ("Personal", "Documents"),
                [new Guid("a8cdff1c-4878-43be-b5fd-f8091c1c60d0")] = ("Personal", "Documents"),
                [new Guid("450d8fba-ad25-11d0-98a8-0800361b1103")] = ("Personal", "Documents"),
                [new Guid("fdd39ad0-238f-46af-adb4-6c85480369c7")] = ("Personal", "Documents"),
                [new Guid("088e3905-0323-4b02-9826-5d99428e115f")] = ("{374DE290-123F-4565-9164-39C4925E467B}", "Downloads"),
                [new Guid("374de290-123f-4565-9164-39c4925e467b")] = ("{374DE290-123F-4565-9164-39C4925E467B}", "Downloads"),
                [new Guid("24ad3ad4-a569-4530-98e1-ab02f9417aa8")] = ("My Pictures", "Pictures"),
                [new Guid("3add1653-eb32-4cb0-bbd7-dfa0abb5acca")] = ("My Pictures", "Pictures"),
                [new Guid("33e28130-4e1e-4676-835a-98395c3bc3bb")] = ("My Pictures", "Pictures"),
                [new Guid("3dfdf296-dbec-4fb4-81d1-6a3438bcf4de")] = ("My Music", "Music"),
                [new Guid("1cf1260c-4dd0-4ebb-811f-33c572699fde")] = ("My Music", "Music"),
                [new Guid("4bd8d571-6d19-48d3-be97-422220080e43")] = ("My Music", "Music"),
                [new Guid("f86fa3ab-70d2-4fc7-9c99-fcbf05467f3a")] = ("My Video", "Videos"),
                [new Guid("a0953c92-50dc-43bf-be83-3742fed03c9c")] = ("My Video", "Videos"),
                [new Guid("18989b1d-99b5-455b-841c-ab7c74e4ddfc")] = ("My Video", "Videos"),
                [new Guid("1777f761-68ad-4d8a-87bd-30b759fa33dd")] = ("Favorites", "Favorites"),
                // Chiavi derivate (non presenti in Shell Folders): vedi AddDerivedFolders
                [new Guid("59031a47-3f72-44a7-89c5-5595fe6b30ee")] = (UserProfileKey, "%USERPROFILE%"),
                [new Guid("a52bba46-e9e1-435f-b3d9-28daa648c0f6")] = (OneDriveKey, "OneDrive"),     // FOLDERID_SkyDrive
                [new Guid("018d5c66-4533-4307-9b53-224de2ed1fe6")] = (OneDriveKey, "OneDrive"),     // CLSID OneDrive
            };

        private const string UserProfileKey = "*UserProfile";
        private const string OneDriveKey = "*OneDrive";

        /// <summary>
        /// Aggiunge alla mappa le cartelle non presenti in Shell Folders: profilo utente (da AppData)
        /// e OneDrive (da HKCU\Environment\OneDrive).
        /// </summary>
        private static void AddDerivedFolders(Dictionary<string, string> map, string oneDrivePath)
        {
            const string roaming = @"\AppData\Roaming";
            if (map.TryGetValue("AppData", out var appData) && appData.EndsWith(roaming, StringComparison.OrdinalIgnoreCase))
                map[UserProfileKey] = appData.Substring(0, appData.Length - roaming.Length);
            if (!string.IsNullOrWhiteSpace(oneDrivePath) && IsAbsoluteFolderPath(oneDrivePath))
                map[OneDriveKey] = oneDrivePath.TrimEnd('\\');
        }

        private static readonly Guid ThisPcClsid = new Guid("20d04fe0-3aea-1069-a2d8-08002b30309d");

        /// <summary>Explorer\Shell Folders da hive offline (valori REG_SZ già espansi dall'OS).</summary>
        internal static Dictionary<string, string> ReadShellFolders(Registry.RegistryHive hive)
        {
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var key = GetKeyInsensitive(hive, AppConstants.Registry.ShellFoldersPath);
            if (key == null) return map;
            foreach (var v in key.Values)
            {
                if (!string.IsNullOrWhiteSpace(v.ValueData) && IsAbsoluteFolderPath(v.ValueData))
                    map[v.ValueName] = v.ValueData.TrimEnd('\\');
            }
            var env = GetKeyInsensitive(hive, "Environment");
            var oneDrive = env?.Values.FirstOrDefault(v => string.Equals(v.ValueName, "OneDrive", StringComparison.OrdinalIgnoreCase))?.ValueData;
            AddDerivedFolders(map, oneDrive);
            return map;
        }

        /// <summary>Explorer\Shell Folders dal registro live dell'utente corrente.</summary>
        private static Dictionary<string, string> ReadShellFoldersLive()
        {
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(AppConstants.Registry.ShellFoldersPath))
                {
                    if (key == null) return map;
                    foreach (var name in key.GetValueNames())
                    {
                        if (key.GetValue(name) is string s && IsAbsoluteFolderPath(s))
                            map[name] = s.TrimEnd('\\');
                    }
                }
                using (var env = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("Environment"))
                {
                    AddDerivedFolders(map, env?.GetValue("OneDrive") as string);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ReadShellFoldersLive] {ex.Message}");
            }
            return map;
        }

        private static bool IsAbsoluteFolderPath(string s)
        {
            return s.Length >= 3 && char.IsLetter(s[0]) && s[1] == ':' && s[2] == '\\'
                && s.IndexOfAny(Path.GetInvalidPathChars()) < 0;
        }

        /// <summary>
        /// Componente radice di un root folder item (0x1F): percorso reale della cartella nota se noto,
        /// altrimenti nome generico; null per Questo PC o GUID sconosciuti.
        /// </summary>
        private static string ResolveRootFolder(byte[] itemData, IReadOnlyDictionary<string, string> shellFolders)
        {
            // itemData: [sortIndex:1][CLSID:16]...
            if (itemData.Length < 17) return null;
            return ResolveKnownFolder(ReadGuid(itemData, 1), shellFolders);
        }

        /// <summary>
        /// Item 0x00 con firma 0x23FEBBEE: riferimento a una known folder (es. FOLDERID_SkyDrive sotto il profilo).
        /// Layout itemData: [?:1][size:2][sig:4][?:4][KNOWNFOLDERID:16]
        /// </summary>
        private static string ResolveKnownFolderItem(byte[] itemData, IReadOnlyDictionary<string, string> shellFolders)
        {
            if (itemData.Length < 27 || BitConverter.ToUInt32(itemData, 3) != 0x23FEBBEE) return null;
            return ResolveKnownFolder(ReadGuid(itemData, 11), shellFolders);
        }

        private static Guid ReadGuid(byte[] data, int offset)
        {
            var bytes = new byte[16];
            Buffer.BlockCopy(data, offset, bytes, 0, 16);
            return new Guid(bytes);
        }

        private static string ResolveKnownFolder(Guid id, IReadOnlyDictionary<string, string> shellFolders)
        {
            if (id == ThisPcClsid) return null;
            if (!KnownFolderRoots.TryGetValue(id, out var known)) return null;
            if (shellFolders != null && shellFolders.TryGetValue(known.ShellFolderValue, out var path))
                return path;
            return known.DisplayName;
        }

        /// <summary>Aggiunge un componente cartella nota: se è un percorso assoluto sostituisce quanto accumulato.</summary>
        private static void AddFolderComponent(List<string> components, string folder)
        {
            if (IsAbsoluteFolderPath(folder)) components.Clear();
            components.Add(folder);
        }

        private static string ParseShellItemPath(byte[] data) => ParseShellItemPath(data, null);

        private static string ParseShellItemPath(byte[] data, IReadOnlyDictionary<string, string> shellFolders)
        {
            if (data == null || data.Length < 4)
            {
                System.Diagnostics.Debug.WriteLine($"[ParseShellItemPath] Data too short: {data?.Length}");
                return null;
            }

            try
            {
                // Parse shell item ID list: each item is [Size:2][Type:1][Data...], terminated by 00 00
                // The full path is built by concatenating names from ALL items in the ID list
                int offset = 0;
                var pathComponents = new List<string>();
                bool rootSeen = false; // radice, volume o primo file entry già incontrati

                while (offset < data.Length - 1)
                {
                    // Read item size (2 bytes, little-endian)
                    if (offset + 1 >= data.Length) break;
                    var itemSize = BitConverter.ToUInt16(data, offset);
                    if (itemSize == 0) break;  // Terminator (00 00)
                    if (itemSize < 3 || offset + itemSize > data.Length) break;

                    // Item type is at offset + 2
                    var itemType = data[offset + 2];
                    
                    // Item data starts at offset + 3
                    var itemDataSize = itemSize - 3;
                    if (itemDataSize > 0)
                    {
                        var itemData = new byte[itemDataSize];
                        Buffer.BlockCopy(data, offset + 3, itemData, 0, itemDataSize);

                        // Parse per class type (per libfwsi)
                        string component = null;
                        bool componentTrusted = false; // letto da offset strutturale: ammette qualsiasi alfabeto

                        if (itemType == 0x1F) // Root folder - resolved via GUID
                        {
                            rootSeen = true;
                            var root = ResolveRootFolder(itemData, shellFolders);
                            if (root != null)
                            {
                                // Percorso assoluto (C:\Users\...\Documents) o nome generico: aggiunto senza filtri
                                AddFolderComponent(pathComponents, root);
                                offset += itemSize;
                                continue;
                            }
                        }
                        else if (itemType == 0x00)
                        {
                            var folder = ResolveKnownFolderItem(itemData, shellFolders);
                            if (folder != null)
                            {
                                AddFolderComponent(pathComponents, folder);
                                offset += itemSize;
                                continue;
                            }
                        }
                        else if (itemType >= 0x30 && itemType <= 0x3F) // File entry (0x31=folder, 0x32=file, 0x35/0x36=Unicode)
                        {
                            component = ParseFileEntryName(itemData, itemType, out componentTrusted);
                            // ID list relativo alla radice del namespace (Desktop): un file entry senza radice
                            // né volume davanti sta sul Desktop (es. salvataggio diretto sul Desktop)
                            if (!rootSeen && pathComponents.Count == 0 && shellFolders != null
                                && shellFolders.TryGetValue("Desktop", out var desktop))
                            {
                                pathComponents.Add(desktop);
                            }
                            rootSeen = true;
                        }
                        else if (itemType >= 0x20 && itemType <= 0x2F) // Volume
                        {
                            // Nome volume ASCII ("C:\") subito dopo il class type, cioè itemData[0]
                            int len = 0;
                            while (len < itemData.Length && len < 20 && itemData[len] != 0) len++;
                            var volume = Encoding.ASCII.GetString(itemData, 0, len).TrimEnd('\\', ' ');
                            if (volume.Length == 2 && char.IsLetter(volume[0]) && volume[1] == ':')
                            {
                                rootSeen = true;
                                pathComponents.Add(volume.ToUpperInvariant());
                                offset += itemSize;
                                continue;
                            }
                        }
                        else if (itemType >= 0x40 && itemType <= 0x4F) // Network location
                        {
                            if (itemData.Length > 5)
                            {
                                var locBytes = new byte[itemData.Length - 5];
                                Buffer.BlockCopy(itemData, 5, locBytes, 0, locBytes.Length);
                                component = Encoding.ASCII.GetString(locBytes).TrimEnd('\0');
                            }
                        }
                        else if (itemType == 0x74) // Delegate folder - contains embedded file entry
                        {
                            component = ParseDelegateFolder(itemData, out componentTrusted);
                        }

                        if (!string.IsNullOrEmpty(component) &&
                            (componentTrusted ? IsPlausibleFileName(component) : IsValidPathComponent(component)))
                        {
                            pathComponents.Add(component);
                        }
                    }

                    rootSeen = true;
                    offset += itemSize;
                }

                // Build full path by concatenating all components
                if (pathComponents.Count > 0)
                {
                    var fullPath = string.Join("\\", pathComponents);
                    if (fullPath.Length == 2 && fullPath[1] == ':') fullPath += "\\"; // solo volume → "C:\"
                    return fullPath;
                }

                // FALLBACK: Search entire data for any valid path string
                var paths = ExtractAllValidPaths(data);
                if (paths.Count > 0)
                {
                    var bestPath = paths.OrderByDescending(p => p.Length).First();
                    return bestPath;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ParseShellItemPath] Exception: {ex.Message}");
            }
            return null;
        }

        private static string ParseFileEntryName(byte[] itemData, byte itemType)
            => ParseFileEntryName(itemData, itemType, out _);

        private static string ParseFileEntryName(byte[] itemData, byte itemType, out bool trusted)
        {
            trusted = false;
            // File entry (0x30-0x3F): [flags:1][pad:1][fileSize:4][mtime:4][attr:2][name...]
            // Name at offset 14 from item start. itemData starts after Size(2)+Type(1)=3 bytes, so offset in itemData = 11
            if (itemData.Length < 14) return null;

            bool isUnicode = (itemType & 0x04) != 0;
            int nameOffset = 11; // 14 - 3 (was 12, off by 1 wchar -> ONTHL~1 vs MONTHL~1)
            
            if (nameOffset >= itemData.Length) return null;

            string shortName = null;
            int nameEndOffset = nameOffset;

            if (isUnicode)
            {
                var nameBytes = new byte[itemData.Length - nameOffset];
                Buffer.BlockCopy(itemData, nameOffset, nameBytes, 0, nameBytes.Length);
                shortName = Encoding.Unicode.GetString(nameBytes).TrimEnd('\0');
                // Find real null terminator for accurate end offset
                int nullPos = Array.IndexOf(nameBytes, (byte)0);
                if (nullPos >= 0 && nullPos % 2 == 1) nullPos--; // align
                nameEndOffset = nameOffset + (nullPos >= 0 ? nullPos + 2 : nameBytes.Length);
            }
            else
            {
                var nameBytes = new List<byte>();
                for (int i = nameOffset; i < itemData.Length && itemData[i] != 0; i++)
                    nameBytes.Add(itemData[i]);
                shortName = Encoding.ASCII.GetString(nameBytes.ToArray());
                nameEndOffset = nameOffset + shortName.Length + 1;
            }

            // Try to find extension block 0xBEEF0004 for long name
            string longName = TryParseExtensionBlockBeef0004(itemData, nameEndOffset, out bool longNameTrusted);
            if (!string.IsNullOrWhiteSpace(longName))
            {
                trusted = longNameTrusted;
                return longName;
            }
            return shortName;
        }

        private static string TryParseExtensionBlockBeef0004(byte[] itemData, int startOffset)
            => TryParseExtensionBlockBeef0004(itemData, startOffset, out _);

        /// <param name="trusted">true se il nome è stato letto all'offset strutturale e validato
        /// (può contenere qualsiasi alfabeto); false se proviene dalla scansione euristica.</param>
        private static string TryParseExtensionBlockBeef0004(byte[] itemData, int startOffset, out bool trusted)
        {
            trusted = false;
            string bestLongName = null;

            for (int i = 0; i + 7 < itemData.Length; i++)
            {
                uint sig = BitConverter.ToUInt32(itemData, i);
                if (sig != 0xBEEF0004) continue;

                if (i < 4) continue;
                int sizeOffset = i - 4;
                ushort extSize = BitConverter.ToUInt16(itemData, sizeOffset);
                ushort extVersion = BitConverter.ToUInt16(itemData, sizeOffset + 2);
                if (extSize < 8 || sizeOffset + extSize > itemData.Length) continue;

                int dataOffset = sizeOffset + 8;
                int dataSize = extSize - 8;
                if (dataSize <= 0 || dataOffset + dataSize > itemData.Length) continue;

                // Lettura strutturale (libfwsi): offset del long name noto dalla versione del blocco.
                // Affidabile per qualsiasi alfabeto; la scansione euristica resta come fallback.
                string structural = TryReadStructuralLongName(itemData, sizeOffset, extSize, extVersion);
                if (structural != null)
                {
                    trusted = true;
                    return structural;
                }

                string candidate = ExtractLongNameFromBlock(itemData, dataOffset, dataSize, sizeOffset, extVersion);
                if (!string.IsNullOrEmpty(candidate) && HasValidExtension(candidate))
                {
                    return candidate; // first valid filename with extension is the correct one
                }
                if (!string.IsNullOrEmpty(candidate) && bestLongName == null)
                    bestLongName = candidate;
            }
            return bestLongName;
        }

        /// <summary>
        /// Legge il long name all'offset documentato del blocco 0xBEEF0004 e ne verifica la coerenza strutturale:
        /// versione nota (3..9), terminatore dentro il blocco e, se non c'è nome localizzato (longStringSize == 0),
        /// il blocco deve chiudersi esattamente con il campo finale da 2 byte (first extension block offset).
        /// </summary>
        internal static string TryReadStructuralLongName(byte[] itemData, int blockStart, int blockSize, ushort version)
        {
            if (version < 3 || version > 9) return null; // layout non documentato: lascia decidere all'euristica
            int nameOffset = GetBeef0004LongNameOffset(version);
            int blockEnd = blockStart + blockSize;
            int nameStart = blockStart + nameOffset;
            int trailerStart = blockEnd - 2;
            if (blockStart < 0 || blockEnd > itemData.Length || nameStart >= trailerStart) return null;

            int nameEnd = -1;
            for (int o = nameStart; o + 1 < trailerStart; o += 2)
            {
                if (itemData[o] == 0 && itemData[o + 1] == 0) { nameEnd = o; break; }
            }
            if (nameEnd <= nameStart) return null;

            int longStringSizeOffset = blockStart + (version >= 7 ? 36 : 18);
            ushort longStringSize = BitConverter.ToUInt16(itemData, longStringSizeOffset);
            if (longStringSize == 0 && nameEnd + 2 != trailerStart) return null;

            string name = Encoding.Unicode.GetString(itemData, nameStart, nameEnd - nameStart);
            return IsPlausibleFileName(name) ? name : null;
        }

        private static readonly char[] InvalidFileNameChars = Path.GetInvalidFileNameChars();

        /// <summary>
        /// Validazione per nomi letti da posizione strutturale certa: accetta qualsiasi alfabeto
        /// (cirillico, CJK, accenti...) e rifiuta solo ciò che Windows non ammette in un nome file.
        /// </summary>
        internal static bool IsPlausibleFileName(string s)
        {
            if (string.IsNullOrWhiteSpace(s) || s.Length > 255) return false;
            if (s.IndexOfAny(InvalidFileNameChars) >= 0) return false;
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (char.IsControl(c) || c == '�' || c == '￾' || c == '￿') return false;
                if (char.IsHighSurrogate(c))
                {
                    if (i + 1 >= s.Length || !char.IsLowSurrogate(s[i + 1])) return false;
                    i++;
                }
                else if (char.IsLowSurrogate(c)) return false;
            }
            return true;
        }

        private static bool HasValidExtension(string s)
        {
            var ext = System.IO.Path.GetExtension(s);
            return !string.IsNullOrEmpty(ext) && ext.Length >= 2 && ext.Length <= 10;
        }
        
        private static void DumpAround(byte[] data, int offset)
        {
            if (!_beefResidualDiagnosticsEnabled) return;
            int start = Math.Max(0, offset - 32);
            int len = Math.Min(96, data.Length - start);
            string hex = BitConverter.ToString(data, start, len);
            System.Diagnostics.Debug.WriteLine($"[TryParseExtensionBlockBeef0004] Dump @0x{start:X} (found @0x{offset:X}): {hex}");
        }

        private static void LogResidualContext(byte[] data, int candidateOffset, string candidate)
        {
            if (!_beefResidualDiagnosticsEnabled) return;
            // 16-32 bytes before the candidate + preceding WCHAR at offset-2
            int ctxStart = Math.Max(0, candidateOffset - 32);
            int ctxLen = Math.Min(64, data.Length - ctxStart);
            string hex = BitConverter.ToString(data, ctxStart, ctxLen);
            string prevWchar = candidateOffset >= 2 && candidateOffset + 1 < data.Length
                ? $"prevWCHAR bytes={data[candidateOffset - 2]:X2}-{data[candidateOffset - 1]:X2} char='{(char)BitConverter.ToUInt16(data, candidateOffset - 2)}' (U+{BitConverter.ToUInt16(data, candidateOffset - 2):X4})"
                : "prevWCHAR n/a";
            System.Diagnostics.Debug.WriteLine($"[TryParseExtensionBlockBeef0004][RESIDUAL] candidate @0x{candidateOffset:X}: '{candidate}' | {prevWchar} | ctx[{ctxStart:X}..]: {hex}");
        }
        
        private static void ScanUtf16InRange(byte[] data, int start, int maxLen)
        {
            if (!_beefResidualDiagnosticsEnabled) return;
            int end = Math.Min(start + maxLen, data.Length - 2);
            for (int o = start; o < end; o += 2)
            {
                try
                {
                    int remaining = data.Length - o;
                    if (remaining < 4) break;
                    int maxChars = Math.Min(260, remaining / 2);
                    var testBytes = new byte[maxChars * 2];
                    Buffer.BlockCopy(data, o, testBytes, 0, testBytes.Length);
                    string s = Encoding.Unicode.GetString(testBytes);
                    int nullIdx = s.IndexOf('\0');
                    if (nullIdx >= 4 && nullIdx <= 260)
                    {
                        string candidate = s.Substring(0, nullIdx);
                        if (IsValidPathComponent(candidate) && candidate.Length > 3)
                        {
                            System.Diagnostics.Debug.WriteLine($"[TryParseExtensionBlockBeef0004] UNICODE @0x{o:X}: '{candidate}'");
                        }
                    }
                }
                catch { }
            }
        }
        
        /// <summary>
        /// Offset del long name (UTF-16LE) relativo all'inizio del blocco 0xBEEF0004, secondo libfwsi:
        /// size(2) version(2) sig(4) ctime(4) atime(4) identifier(2, v>=3)
        /// [v>=7: empty(2) fileRef(8) unknown(8)] longStringSize(2, v>=3) [v>=9: unknown(4)] [v>=8: unknown(4)] longName.
        /// Ritorna -1 se la versione non prevede un long name.
        /// </summary>
        internal static int GetBeef0004LongNameOffset(ushort version)
        {
            if (version < 3) return -1;
            int offset = 18;                 // header + ctime + atime + identifier
            if (version >= 7) offset += 18;  // empty + file reference + unknown
            offset += 2;                     // long string size
            if (version >= 9) offset += 4;
            if (version >= 8) offset += 4;
            return offset;
        }

        private static string ExtractLongNameFromBlock(byte[] itemData, int startOffset, int maxLen, int blockStart = -1, ushort blockVersion = 0)
        {
            int end = Math.Min(startOffset + maxLen, itemData.Length - 4);
            string bestCandidate = null;
            int bestOffset = -1;
            int bestScore = int.MinValue;
            string runnerUp = null;
            int runnerUpOffset = -1;
            int runnerUpScore = int.MinValue;
            var allCandidates = new System.Collections.Generic.List<System.Tuple<int, string>>();
            for (int o = startOffset; o < end; o += 2)
            {
                try
                {
                    int remaining = itemData.Length - o;
                    if (remaining < 4) break;

                    // Il nome reale inizia quasi sempre con ASCII + 00 (es. 46 00 = F, 53 00 = S).
                    if (itemData[o + 1] != 0x00) continue;
                    char firstChar = (char)itemData[o];
                    if (!IsValidFilenameStart(firstChar)) continue;

                    int maxChars = Math.Min(260, remaining / 2);
                    var buffer = new byte[maxChars * 2];
                    Buffer.BlockCopy(itemData, o, buffer, 0, buffer.Length);
                    string unicode = Encoding.Unicode.GetString(buffer);
                    int nullIndex = unicode.IndexOf('\0');
                    if (nullIndex < 2 || nullIndex > 260) continue;
                    string candidate = unicode.Substring(0, nullIndex).Trim();
                    if (!IsValidLongName(candidate)) continue;

                    int score = ScoreCandidate(candidate);
                    allCandidates.Add(System.Tuple.Create(o, candidate));
                    if (score > bestScore)
                    {
                        runnerUp = bestCandidate;
                        runnerUpOffset = bestOffset;
                        runnerUpScore = bestScore;
                        bestScore = score;
                        bestCandidate = candidate;
                        bestOffset = o;
                    }
                    else if (score > runnerUpScore)
                    {
                        runnerUp = candidate;
                        runnerUpOffset = o;
                        runnerUpScore = score;
                    }
                }
                catch { }
            }
            if (!string.IsNullOrWhiteSpace(bestCandidate))
            {
                if (_beefResidualDiagnosticsEnabled)
                {
                    System.Diagnostics.Debug.WriteLine($"[TryParseExtensionBlockBeef0004] BEST long name @0x{bestOffset:X} (score={bestScore}): '{bestCandidate}'");
                    LogResidualContext(itemData, bestOffset, bestCandidate);
                    if (!string.IsNullOrEmpty(runnerUp) && runnerUpOffset >= 0 && System.Math.Abs(runnerUpOffset - bestOffset) == 2)
                    {
                        System.Diagnostics.Debug.WriteLine($"[TryParseExtensionBlockBeef0004][RESIDUAL] runner-up @0x{runnerUpOffset:X} (score={runnerUpScore}): '{runnerUp}'");
                        LogResidualContext(itemData, runnerUpOffset, runnerUp);
                    }
                    // Pattern X+Nome vs Nome: cerca candidati dove shifted è contenuto negli altri candidati
                    var candidateSet = new System.Collections.Generic.HashSet<string>(allCandidates.Select(t => t.Item2));
                    foreach (var t in allCandidates)
                    {
                        if (t.Item2.Length > 3)
                        {
                            string shifted = t.Item2.Substring(1);
                            if (candidateSet.Contains(shifted))
                            {
                                System.Diagnostics.Debug.WriteLine($"[TryParseExtensionBlockBeef0004][RESIDUAL][SHIFT-PATTERN] '{t.Item2}' @0x{t.Item1:X} contains shifted '{shifted}'");
                                DumpCandidateBytes(itemData, t.Item1);
                                int shiftedOffset = allCandidates.First(x => x.Item2 == shifted).Item1;
                                DumpCandidateBytes(itemData, shiftedOffset);
                            }
                        }
                    }
                }

                // Selezione strutturale: se il best inizia prima dell'offset documentato del long name
                // (ha inglobato un WCHAR dei metadati, es. 'UReport...'), usa il candidato all'offset
                // atteso, purché sia un suffisso del best (stesso terminatore, solo prefisso spurio in meno).
                int nameOffsetInBlock = GetBeef0004LongNameOffset(blockVersion);
                if (blockStart >= 0 && nameOffsetInBlock >= 0)
                {
                    int expectedOffset = blockStart + nameOffsetInBlock;
                    if (bestOffset < expectedOffset)
                    {
                        var structural = allCandidates.FirstOrDefault(t => t.Item1 == expectedOffset);
                        if (structural != null && bestCandidate.EndsWith(structural.Item2, StringComparison.Ordinal))
                        {
                            if (_beefResidualDiagnosticsEnabled)
                                System.Diagnostics.Debug.WriteLine($"[TryParseExtensionBlockBeef0004][RESIDUAL][STRUCTURAL] v{blockVersion}: '{bestCandidate}' @0x{bestOffset:X} -> '{structural.Item2}' @0x{expectedOffset:X}");
                            return structural.Item2;
                        }
                    }
                }
                return bestCandidate;
            }
            return null;
        }

        private static void DumpCandidateBytes(byte[] itemData, int candidateOffset)
        {
            if (!_beefResidualDiagnosticsEnabled) return;
            int start = System.Math.Max(0, candidateOffset - 16);
            int end = System.Math.Min(itemData.Length, candidateOffset + 32);
            string hex = BitConverter.ToString(itemData, start, end - start);
            ushort prev = candidateOffset >= 2 ? BitConverter.ToUInt16(itemData, candidateOffset - 2) : (ushort)0;
            ushort first = candidateOffset + 1 < itemData.Length ? BitConverter.ToUInt16(itemData, candidateOffset) : (ushort)0;
            System.Diagnostics.Debug.WriteLine($"[TryParseExtensionBlockBeef0004][RESIDUAL][DUMP] offset=0x{candidateOffset:X} prev=0x{prev:X4} '{(char)prev}' first=0x{first:X4} '{(char)first}' bytes[{start:X}..{end:X}]: {hex}");
        }

        private static bool IsValidFilenameStart(char c)
        {
            return char.IsLetterOrDigit(c) || c == '#' || c == '_';
        }

        private static bool IsValidLongName(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return false;
            if (s.Length < 3) return false;
            if (HasSuspiciousChars(s)) return false;
            if (s.Any(char.IsControl)) return false;
            return true;
        }

        private static int ScoreCandidate(string s)
        {
            int score = s.Length;
            int lenScore = s.Length;
            int dotScore = s.Contains(".") ? 50 : 0;
            int extScore = System.Text.RegularExpressions.Regex.IsMatch(s, @"\.(txt|pdf|png|jpg|jpeg|webp|xlsx|xls|doc|docx)$", System.Text.RegularExpressions.RegexOptions.IgnoreCase) ? 100 : 0;
            int asciiStartScore = (s[0] <= 127 && char.IsLetterOrDigit(s[0])) ? 100 : (char.IsLetterOrDigit(s[0]) ? 50 : 0);
            int letterScore = s.Count(char.IsLetter) > 3 ? 20 : 0;
            int suspiciousPenalty = HasSuspiciousChars(s) ? -1000 : 0;
            
            score = lenScore + dotScore + extScore + asciiStartScore + letterScore + suspiciousPenalty;
            
            if (_beefResidualDiagnosticsEnabled)
            {
                System.Diagnostics.Debug.WriteLine($"[TryParseExtensionBlockBeef0004][RESIDUAL][SCORE] '{s}': len={lenScore} dot={dotScore} ext={extScore} asciiStart={asciiStartScore} letters={letterScore} suspicious={suspiciousPenalty} = {score}");
            }
            return score;
        }

        private static bool HasSuspiciousChars(string s)
        {
            foreach (char c in s)
            {
                // Any char beyond Latin Extended-B is suspicious for a filename
                // U+A228 (ꨞ), U+A700 etc. are Yi/Bamum and not valid in filenames
                if (c > 0x024F) return true;
                // Also reject control chars
                if (c < 0x20 && c != '\0') return true;
            }
            return false;
        }
        
        private static string TryParseExtensionBlockBeef0004Original(byte[] itemData, int startOffset)
        {
            // Original parsing logic as fallback
            for (int offset = 0; offset + 3 < itemData.Length; offset++)
            {
                int sigLength = 0;
                
                if (itemData[offset] == 0xEF && itemData[offset + 1] == 0xBE)
                    sigLength = 2;
                else if (offset + 3 < itemData.Length && 
                         itemData[offset] == 0x04 && itemData[offset + 1] == 0x00 && 
                         itemData[offset + 2] == 0xEF && itemData[offset + 3] == 0xBE)
                    sigLength = 4;
                else
                    continue;

                int sizeOffset = offset + sigLength;
                if (sizeOffset + 1 >= itemData.Length) continue;
                
                int blockSize = BitConverter.ToUInt16(itemData, sizeOffset);
                if (blockSize < sigLength + 4 || offset + blockSize > itemData.Length) continue;

                int headerSize = sigLength + 2 + 1 + 1;
                int longNameOffset = offset + headerSize;
                longNameOffset = (longNameOffset + 1) & ~1;
                
                int longNameSize = blockSize - headerSize;
                if (longNameSize < 2 || longNameOffset + longNameSize > itemData.Length) 
                {
                    var fallbackName = TryExtractLongNameFallback(itemData, longNameOffset);
                    if (!string.IsNullOrEmpty(fallbackName))
                    {
                        System.Diagnostics.Debug.WriteLine($"[TryParseExtensionBlockBeef0004] Fallback long name: '{fallbackName}' (offset={offset})");
                        return fallbackName;
                    }
                    continue;
                }

                var longNameBytes = new byte[longNameSize];
                Buffer.BlockCopy(itemData, longNameOffset, longNameBytes, 0, longNameSize);
                var longName = Encoding.Unicode.GetString(longNameBytes).TrimEnd('\0');
                
                if (!string.IsNullOrEmpty(longName) && longName.Length > 3 && IsValidPathComponent(longName))
                {
                    System.Diagnostics.Debug.WriteLine($"[TryParseExtensionBlockBeef0004] Long name: '{longName}' (offset={offset}, sigLen={sigLength}, blockSize={blockSize})");
                    return longName;
                }
                
                var fallbackName2 = TryExtractLongNameFallback(itemData, longNameOffset);
                if (!string.IsNullOrEmpty(fallbackName2))
                {
                    System.Diagnostics.Debug.WriteLine($"[TryParseExtensionBlockBeef0004] Fallback2 long name: '{fallbackName2}' (offset={offset})");
                    return fallbackName2;
                }
                
                break;
            }
            
            return null;
        }

        private static string TryExtractLongNameFallback(byte[] itemData, int startOffset)
        {
            // Scan forward from startOffset for a valid UTF-16LE long name
            // Max reasonable long name length: 260 chars = 520 bytes
            int maxScan = Math.Min(itemData.Length - startOffset, 520);
            
            for (int offset = startOffset; offset < startOffset + maxScan - 2; offset += 2)
            {
                // Try to read a UTF-16LE string from here
                int remaining = itemData.Length - offset;
                if (remaining < 4) break; // Need at least 2 chars
                
                // Read up to 260 chars or until double null
                int maxChars = Math.Min(260, remaining / 2);
                var testBytes = new byte[maxChars * 2];
                Buffer.BlockCopy(itemData, offset, testBytes, 0, testBytes.Length);
                var testStr = Encoding.Unicode.GetString(testBytes);
                
                // Find first null terminator
                int nullIdx = testStr.IndexOf('\0');
                if (nullIdx >= 4 && nullIdx <= 260) // At least 4 chars, max 260
                {
                    var candidate = testStr.Substring(0, nullIdx);
                    if (IsValidPathComponent(candidate) && candidate.Length > 3)
                    {
                        // Additional validation: should look like a filename/path component
                        if (candidate.Any(c => char.IsLetterOrDigit(c))) // Has alphanumeric
                        {
                            System.Diagnostics.Debug.WriteLine($"[TryExtractLongNameFallback] Found candidate: '{candidate}' at offset {offset}");
                            return candidate;
                        }
                    }
                }
            }
            return null;
        }

        private static string ParseDelegateFolder(byte[] itemData)
            => ParseDelegateFolder(itemData, out _);

        private static string ParseDelegateFolder(byte[] itemData, out bool trusted)
        {
            trusted = false;
            // Delegate folder (0x74): [cb:2][class:1][pad:1][innerSize:4][sig:4][entrySize:2][FILEENTRY][GUIDs...]
            if (itemData.Length < 16) return null;

            int embeddedOffset = 14; // Skip delegate header
            if (itemData.Length < embeddedOffset + 3) return null;

            byte embeddedType = itemData[embeddedOffset]; // class type at start of embedded data
            if (embeddedType >= 0x30 && embeddedType <= 0x3F)
            {
                int embeddedSize = BitConverter.ToUInt16(itemData, embeddedOffset - 2); // size is 2 bytes before class
                if (embeddedSize > 2 && embeddedOffset - 2 + embeddedSize <= itemData.Length)
                {
                    var embeddedData = new byte[embeddedSize];
                    Buffer.BlockCopy(itemData, embeddedOffset - 2, embeddedData, 0, embeddedSize);
                    return ParseFileEntryName(embeddedData, embeddedType, out trusted);
                }
            }
            return null;
        }

        private static List<string> ExtractAllValidPaths(byte[] data)
        {
            var paths = new List<string>();
            
            try
            {
                // Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                
                // Try Unicode (UTF-16 LE)
                var unicodeStr = Encoding.Unicode.GetString(data);
                var unicodePaths = ExtractPathsFromString(unicodeStr);
                paths.AddRange(unicodePaths);
                
                // Try ANSI (code page 1252)
                try
                {
                    var ansiStr = Encoding.GetEncoding(1252).GetString(data);
                    var ansiPaths = ExtractPathsFromString(ansiStr);
                    paths.AddRange(ansiPaths);
                }
                catch { }
            }
            catch { }
            
            return paths.Distinct().ToList();
        }

        private static List<string> ExtractPathsFromString(string str)
        {
            var paths = new List<string>();
            
            // Split by null terminator and other common delimiters
            var candidates = str.Split(new[] { '\0', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var candidate in candidates)
            {
                var trimmed = candidate.Trim();
                if (IsValidPath(trimmed))
                {
                    paths.Add(trimmed);
                }
                else
                {
                    // Also check substrings that might contain paths
                    // e.g., "SomePrefix C:\Path\To\File.txt Suffix"
                    var colonIndex = trimmed.IndexOf(':');
                    if (colonIndex >= 1 && colonIndex < trimmed.Length - 1 && trimmed[colonIndex + 1] == '\\')
                    {
                        // Found "X:\" pattern, extract from there
                        var pathPart = trimmed.Substring(colonIndex - 1);
                        if (IsValidPath(pathPart))
                            paths.Add(pathPart);
                    }
                    
                    // Check for UNC paths
                    var uncIndex = trimmed.IndexOf("\\\\");
                    if (uncIndex >= 0)
                    {
                        var pathPart = trimmed.Substring(uncIndex);
                        if (IsValidPath(pathPart))
                            paths.Add(pathPart);
                    }
                }
            }
            
            return paths.Distinct().ToList();
        }

        // Class type constants per libfwsi
        private const byte CLASS_ROOT_FOLDER = 0x1F;
        private const byte CLASS_VOLUME_MIN = 0x20;
        private const byte CLASS_VOLUME_MAX = 0x2F;
        private const byte CLASS_FILE_ENTRY_MIN = 0x30;
        private const byte CLASS_FILE_ENTRY_MAX = 0x3F;
        private const byte CLASS_NETWORK_MIN = 0x40;
        private const byte CLASS_NETWORK_MAX = 0x4F;
        private const byte CLASS_URI = 0x61;
        private const byte CLASS_DELEGATE = 0x74;
        private const byte CLASS_CONTROL_PANEL = 0x71;

        // Extension block signature for long names
        private const uint EXT_BLOCK_BEEF = 0xBEEF0004;

        private static string ExtractComponentFromShellItem(byte[] itemData, byte itemType)
        {
            if (itemData == null || itemData.Length < 2) return null;

            try
            {
                // Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

                // Class type ranges per libfwsi:
                // 0x1F = root folder (no name, resolved via GUID)
                // 0x20-0x2F = volume (name at offset 3, ASCII null-terminated, 20 bytes)
                // 0x30-0x3F = file entry (0x31=folder, 0x32=file, 0x35/0x36=Unicode, 0xB1=CLSID)
                // 0x40-0x4F = network location
                // 0x61 = URI
                // 0x74 = delegate folder
                // 0x71 = control panel item

                if (itemType == CLASS_ROOT_FOLDER)
                {
                    // Root folder (0x1F) - no name in item, resolved via GUID
                    // Structure: [cb:2][class:1][sortIndex:1][GUID:16][extBlock...]
                    // No name in item, resolved via GUID in registry
                    return null; // Will be resolved via GUID elsewhere if needed
                }

                if (itemType >= CLASS_VOLUME_MIN && itemType <= CLASS_VOLUME_MAX)
                {
                    // Volume (0x20-0x2F): [cb:2][class:1][name:20][unknown:2][GUID:16...]
                    // Name at offset 3, ASCII null-terminated, padded to 20 bytes
                    if (itemData.Length >= 23)
                    {
                        var nameBytes = new byte[20];
                        Buffer.BlockCopy(itemData, 3, nameBytes, 0, 20);
                        var name = Encoding.ASCII.GetString(nameBytes).TrimEnd('\0', ' ');
                        if (!string.IsNullOrEmpty(name) && IsValidPathComponent(name))
                        {
                            return name;
                        }
                    }
                    return null;
                }

                if (itemType >= 0x30 && itemType <= 0x3F)
                {
                    // File entry (0x30-0x3F): 0x31=folder, 0x32=file, 0x35/0x36=Unicode, 0xB1=CLSID
                    // Structure: [cb:2][class:1][pad:1][fileSize:4][mtimeFAT:4][attr:2][name:...]
                    // Name at offset 14, null-terminated
                    // Flag 0x04 = UTF-16LE, otherwise ASCII
                    return ParseFileEntryName(itemData);
                }

                if (itemType >= 0x40 && itemType <= 0x4F)
                {
                    // Network location (0x40-0x4F): [cb:2][class:1][unknown:1][flags:1][location:...]
                    // Location string at offset 5, ASCII null-terminated
                    if (itemData.Length >= 6)
                    {
                        var locationBytes = new byte[itemData.Length - 5];
                        Buffer.BlockCopy(itemData, 5, locationBytes, 0, locationBytes.Length);
                        var path = Encoding.ASCII.GetString(locationBytes).TrimEnd('\0');
                        if (!string.IsNullOrEmpty(path) && IsValidPath(path))
                        {
                            return path;
                        }
                    }
                    return null;
                }

                if (itemType == 0x61)
                {
                    // URI (0x61): [cb:2][class:1][flags:1][dataSize:2][...][URI string]
                    // URI string at variable offset, try to find it
                    return ExtractUriFromItemData(itemData);
                }

                if (itemType == 0x74)
                {
                    // Delegate folder (0x74): contains embedded file entry
                    // Structure: [cb:2][class:1][pad:1][innerSize:4][signature:4][fileEntrySize:2][FILE ENTRY EMBEDDED][GUIDs...]
                    return ParseDelegateFolder(itemData);
                }

                if (itemType == 0x71)
                {
                    // Control panel item (0x71): GUID at offset 14
                    return null; // No path component
                }

                // For other types, try generic extraction but with stricter validation
                return ExtractGenericComponent(itemData);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ExtractComponentFromShellItem] Exception: {ex.Message}");
            }
            return null;
        }

        private static string ParseFileEntryName(byte[] itemData)
        {
            // File entry (0x30-0x3F): [cb:2][class:1][pad:1][fileSize:4][mtimeFAT:4][attr:2][name...]
            // Name at offset 14, null-terminated
            // Flag in class type: 0x04 bit = Unicode (0x35/0x36), otherwise ASCII
            if (itemData.Length < 16) return null;

            int nameOffset = 14;
            if (nameOffset >= itemData.Length) return null;

            // Check if UTF-16LE (class type 0x35, 0x36 have bit 0x04 set)
            byte classType = itemData.Length > 2 ? itemData[2] : (byte)0;
            bool isUnicode = (classType & 0x04) != 0;

            if (isUnicode)
            {
                // UTF-16LE null-terminated string
                var nameBytes = new byte[itemData.Length - nameOffset];
                Buffer.BlockCopy(itemData, nameOffset, nameBytes, 0, nameBytes.Length);
                var name = Encoding.Unicode.GetString(nameBytes).TrimEnd('\0');
                if (!string.IsNullOrEmpty(name) && IsValidPathComponent(name))
                    return name;
            }
            else
            {
                // ASCII null-terminated string
                var nameBytes = new List<byte>();
                for (int i = nameOffset; i < itemData.Length && itemData[i] != 0; i++)
                    nameBytes.Add(itemData[i]);
                if (nameBytes.Count > 0)
                {
                    var name = Encoding.ASCII.GetString(nameBytes.ToArray());
                    if (!string.IsNullOrEmpty(name) && IsValidPathComponent(name))
                        return name;
                }
            }
            return null;
        }

        private static string ExtractUriFromItemData(byte[] itemData)
        {
            // URI (0x61): try to find URI string in data
            // Search for common URI prefixes
            string dataStr = Encoding.ASCII.GetString(itemData);
            foreach (var prefix in new[] { "http://", "https://", "ftp://", "file://" })
            {
                int idx = dataStr.IndexOf(prefix);
                if (idx >= 0)
                {
                    // Extract until null or end
                    int end = dataStr.IndexOf('\0', idx);
                    if (end < 0) end = dataStr.Length;
                    var uri = dataStr.Substring(idx, end - idx);
                    if (IsValidPathComponent(uri)) return uri;
                }
            }
            return null;
        }

        private static string ExtractGenericComponent(byte[] itemData)
        {
            // Strict generic extraction: only return well-formed path components
            // Search for null-terminated Unicode strings at aligned offsets
            for (int i = 0; i < itemData.Length - 3; i += 2)
            {
                if (i + 1 < itemData.Length && itemData[i] != 0 && itemData[i + 1] == 0)
                {
                    var strBytes = new byte[itemData.Length - i];
                    Buffer.BlockCopy(itemData, i, strBytes, 0, strBytes.Length);
                    var str = Encoding.Unicode.GetString(strBytes).TrimEnd('\0');
                    
                    if (IsValidPathComponent(str) && str.Length >= 2)
                        return str;
                }
            }

            // Fallback: ANSI strings at aligned offsets
            for (int i = 0; i < itemData.Length - 1; i++)
            {
                if (itemData[i] >= 32 && itemData[i] < 127)
                {
                    var start = i;
                    while (i < itemData.Length && itemData[i] >= 32 && itemData[i] < 127) i++;
                    if (i - start >= 2 && i - start < 260)
                    {
                        var str = Encoding.GetEncoding(1252).GetString(itemData, start, i - start);
                        if (IsValidPathComponent(str))
                            return str;
                    }
                }
            }
            return null;
        }

        private static bool IsValidPathComponent(string str)
        {
            if (string.IsNullOrWhiteSpace(str) || str.Length > 260) return false;
            if (str.Contains(":") || str.Contains("\\") || str.Contains("/")) return false;
            if (str.Contains("*") || str.Contains("?") || str.Contains("\"")) return false;
            if (str.Contains("<") || str.Contains(">") || str.Contains("|")) return false;
            if (HasSuspiciousChars(str)) return false;
            int asciiLetters = str.Count(c => char.IsLetterOrDigit(c) || c == '_' || c == '-' || c == '.' || c == ' ' || c == '(' || c == ')');
            double ratio = (double)asciiLetters / str.Length;
            if (ratio < 0.8) return false;
            return true;
        }

        [System.Runtime.InteropServices.DllImport("advapi32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        private static extern int RegQueryInfoKey(Microsoft.Win32.SafeHandles.SafeRegistryHandle hKey,
            IntPtr lpClass, IntPtr lpcchClass, IntPtr lpReserved, IntPtr lpcSubKeys, IntPtr lpcbMaxSubKeyLen,
            IntPtr lpcbMaxClassLen, IntPtr lpcValues, IntPtr lpcbMaxValueNameLen, IntPtr lpcbMaxValueLen,
            IntPtr lpcbSecurityDescriptor, out long lpftLastWriteTime);

        /// <summary>Ultima modifica di una chiave live (Microsoft.Win32.RegistryKey non la espone).</summary>
        private static DateTime? GetKeyLastWriteTime(Microsoft.Win32.RegistryKey key)
        {
            try
            {
                int rc = RegQueryInfoKey(key.Handle, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero,
                    IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, out long fileTime);
                return rc == 0 ? DateTime.FromFileTimeUtc(fileTime).ToLocalTime() : (DateTime?)null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[GetKeyLastWriteTime] {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Byte grezzi di un valore da hive offline. RegistryKey.GetValue della libreria Registry restituisce
        /// i REG_BINARY come stringa esadecimale ("14-00-1F-..."), quindi "as byte[]" darebbe sempre null.
        /// </summary>
        private static byte[] GetRawValue(Registry.Abstractions.RegistryKey key, string valueName)
        {
            return key.Values
                .FirstOrDefault(v => string.Equals(v.ValueName, valueName, StringComparison.OrdinalIgnoreCase))
                ?.ValueDataRaw;
        }

        private static Registry.Abstractions.RegistryKey GetKeyInsensitive(Registry.RegistryHive hive, string path)
        {
            var direct = hive.GetKey(path);
            if (direct != null) return direct;

            Registry.Abstractions.RegistryKey current = hive.Root;
            foreach (var segment in path.Split('\\'))
            {
                current = current?.SubKeys.FirstOrDefault(sk =>
                    string.Equals(sk.KeyName, segment, StringComparison.OrdinalIgnoreCase));
                if (current == null) return null;
            }
            return current;
        }

        private static LnkFile LoadFile(byte[] raw, string lnkFilePath)
        {
            if (raw.Length == 0 || raw[0] != 0x4c) return null;
            return new LnkFile(raw, lnkFilePath);
        }
    }
}