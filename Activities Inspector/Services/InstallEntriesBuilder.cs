using CSharpFunctionalExtensions;
using Microsoft.Win32;
using Activities_Inspector.Constants;
using Activities_Inspector.Models;
using Activities_Inspector.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Activities_Inspector.Services
{
    public class InstallEntriesBuilder : IInstallEntriesBuilder
    {
        public async Task<Result<List<InstallEntry>>> GetInstallEntriesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var wow6432Locals = await GetFromLocalMachineAsync(AppConstants.Registry.Wow6432UninstallPath, cancellationToken);
                var microsoftLocals = await GetFromLocalMachineAsync(AppConstants.Registry.MicrosoftUninstallPath, cancellationToken);
                var users = await GetFromCurrentUserAsync(AppConstants.Registry.MicrosoftUninstallPath, cancellationToken);
                var events = await GetFromEventsAsync(cancellationToken);

                var startMenu = await GetFromStartMenuAsync(cancellationToken);
                var all = wow6432Locals.Concat(microsoftLocals).Concat(users).Concat(events).Concat(startMenu).ToList();
                return Result.Success(DedupeEntries(all));
            }
            catch (Exception ex) when (!(ex is OperationCanceledException))
            {
                return Result.Failure<List<InstallEntry>>(ex.ToString());
            }
        }

        private Task<List<InstallEntry>> GetFromLocalMachineAsync(string keyPath, CancellationToken cancellationToken)
        {
            return Task.Run(() =>
            {
                var entries = new List<InstallEntry>();

                using var rk = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(keyPath);
                if (rk == null) return entries;

                foreach (var skName in rk.GetSubKeyNames())
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    using var sk = rk.OpenSubKey(skName);
                    if (sk == null) continue;

                    var entry = BuildInstallEntry(sk);
                    if (entry != null)
                        entries.Add(entry);
                }

                return entries;
            }, cancellationToken);
        }

        private Task<List<InstallEntry>> GetFromCurrentUserAsync(string keyPath, CancellationToken cancellationToken)
        {
            return Task.Run(() =>
            {
                var entries = new List<InstallEntry>();

                using var rk = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(keyPath);
                if (rk == null) return entries;

                foreach (var skName in rk.GetSubKeyNames())
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    using var sk = rk.OpenSubKey(skName);
                    if (sk == null) continue;

                    var entry = BuildInstallEntry(sk);
                    if (entry != null)
                        entries.Add(entry);
                }

                return entries;
            }, cancellationToken);
        }

        private Task<List<InstallEntry>> GetFromEventsAsync(CancellationToken cancellationToken)
        {
            return Task.Run(() =>
            {
                var events = Helpers.GetLogEntries(AppConstants.EventLog.ApplicationLog).ToList();
#pragma warning disable CS0618 // Come sopra: serve EventID, non InstanceId.
                var installedPrograms = events.Where(ev =>
                    ev.EventID == AppConstants.EventLog.MsiInstallEventId &&
                    ev.Source == AppConstants.EventLog.MsiInstallerProviderName).ToList();
#pragma warning restore CS0618

                var entries = new List<InstallEntry>();

                for (int i = 0; i < installedPrograms.Count; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var message = installedPrograms[i].ReplacementStrings[0];
                    if (string.IsNullOrEmpty(message)) continue;

                    var substrings = message.Split(':');
                    if (substrings.Length < 2) continue;

                    var substrings2 = substrings[1].Split(new[] { '-', '-' }, StringSplitOptions.RemoveEmptyEntries);
                    var fileName = substrings2.Length > 0 ? substrings2[0].Trim() : string.Empty;

                    var entry = new InstallEntry(fileName, string.Empty, string.Empty,
                        installedPrograms[i].TimeGenerated.ToLocalTime());

                    if (entries.Any(ie => ie.FileName == entry.FileName &&
                        ie.InstallDate == entry.InstallDate)) continue;

                    entries.Add(entry);
                }

                return entries;
            }, cancellationToken);
        }

        private static InstallEntry BuildInstallEntry(RegistryKey registryKey)
        {
            var displayName = registryKey.GetValue("DisplayName")?.ToString();

            if (!ShouldInclude(
                displayName,
                registryKey.GetValue("SystemComponent")?.ToString(),
                registryKey.GetValue("ParentKeyName")?.ToString(),
                registryKey.GetValue("ReleaseType")?.ToString()))
            {
                return null;
            }

            return new InstallEntry(
                displayName,
                registryKey.ToString(),
                registryKey.GetValue("InstallLocation")?.ToString(),
                DateBuilder.BuildDateTimeFromString(registryKey.GetValue("InstallDate")?.ToString()));
        }

        internal static bool ShouldInclude(string displayName, string systemComponent, string parentKeyName, string releaseType)
        {
            if (string.IsNullOrWhiteSpace(displayName)) return false;
            if (string.Equals(systemComponent, "1", StringComparison.Ordinal)) return false;
            if (!string.IsNullOrEmpty(parentKeyName)) return false;
            return !IsUpdateRelease(releaseType);
        }

        private static bool IsUpdateRelease(string releaseType)
        {
            if (string.IsNullOrEmpty(releaseType)) return false;

            return releaseType.Equals("Hotfix", StringComparison.OrdinalIgnoreCase)
                || releaseType.Equals("Security Update", StringComparison.OrdinalIgnoreCase)
                || releaseType.Equals("Update", StringComparison.OrdinalIgnoreCase);
        }

        internal static List<InstallEntry> DedupeEntries(List<InstallEntry> entries)
        {
            if (entries == null) return new List<InstallEntry>();

            return entries
                .Where(e => !string.IsNullOrEmpty(e.FileName))
                .GroupBy(e => e.FileName, StringComparer.OrdinalIgnoreCase)
                .Select(MergeGroup)
                .ToList();
        }

        private static InstallEntry MergeGroup(IGrouping<string, InstallEntry> group)
        {
            // Si tiene la riga con percorso registry (prova dell'installazione)
            // e le si innesta la data migliore: così "senza percorso" resta
            // sinonimo di "senza chiave" (stealth o disinstallato).
            var merged = group.FirstOrDefault(e => !string.IsNullOrEmpty(e.FullPath))
                ?? group.First();

            var dated = group.FirstOrDefault(e => e.InstallDate.HasValue);
            if (dated != null)
                merged.InstallDate = dated.InstallDate;

            return merged;
        }

        private Task<List<InstallEntry>> GetFromStartMenuAsync(CancellationToken cancellationToken)
        {
            return Task.Run(() =>
            {
                var entries = new List<InstallEntry>();

                foreach (var root in StartMenuDirectories())
                {
                    foreach (var lnkPath in EnumerateLnkFiles(root))
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        InstallEntry entry = null;

                        try
                        {
                            entry = BuildStartMenuEntry(lnkPath);
                        }
                        catch
                        {
                            continue;
                        }

                        if (entry != null)
                            entries.Add(entry);
                    }
                }

                return entries;
            }, cancellationToken);
        }

        private static IEnumerable<string> StartMenuDirectories()
        {
            var candidates = new[]
            {
                Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu),
                    "Programs"),
                Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),
                    "Programs")
            };

            return candidates.Where(Directory.Exists);
        }

        internal static IEnumerable<string> EnumerateLnkFiles(string root)
        {
            var stack = new Stack<string>();
            stack.Push(root);

            while (stack.Count > 0)
            {
                var dir = stack.Pop();

                string[] subDirs = Array.Empty<string>();
                string[] files = Array.Empty<string>();

                try
                {
                    subDirs = Directory.GetDirectories(dir);
                    files = Directory.GetFiles(dir, "*.lnk");
                }
                catch
                {
                    continue;
                }

                foreach (var file in files)
                    yield return file;

                foreach (var subDir in subDirs)
                    stack.Push(subDir);
            }
        }

        internal static InstallEntry BuildStartMenuEntry(string lnkPath)
        {
            var raw = File.ReadAllBytes(lnkPath);
            if (raw.Length == 0 || raw[0] != 0x4c) return null;

            var lnkFile = new LnkFile(raw, lnkPath);
            var target = RecentFilesService.ResolveTargetPath(
                lnkFile.LocalPath,
                lnkFile.NetworkShareInfo?.NetworkShareName,
                lnkFile.CommonPath);

            if (string.IsNullOrEmpty(target) || !File.Exists(target)) return null;

            FileVersionInfo info;
            try
            {
                info = FileVersionInfo.GetVersionInfo(target);
            }
            catch
            {
                return null;
            }

            var name = !string.IsNullOrWhiteSpace(info.ProductName) ? info.ProductName
                : !string.IsNullOrWhiteSpace(info.FileDescription) ? info.FileDescription
                : Path.GetFileNameWithoutExtension(target);

            return new InstallEntry(name, lnkPath, target, null);
        }
    }
}