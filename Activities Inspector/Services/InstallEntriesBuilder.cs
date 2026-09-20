using CSharpFunctionalExtensions;
using Microsoft.Win32;
using Registry;
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
        private readonly Evidence.IEvidenceSourceProvider _sources;

        public InstallEntriesBuilder(Evidence.IEvidenceSourceProvider sources)
        {
            _sources = sources;
        }

        private static string ApplicationLogPath => Path.Combine(
            Environment.SystemDirectory, "winevt", "Logs", "Application.evtx");

        public IReadOnlyList<IntegrityRecord> LastIntegrityManifest { get; private set; }
            = new List<IntegrityRecord>();

        public async Task<Result<List<InstallEntry>>> GetInstallEntriesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var manifest = new List<IntegrityRecord>();
                LastIntegrityManifest = manifest;

                List<InstallEntry> wow6432Locals;
                List<InstallEntry> microsoftLocals;
                List<InstallEntry> users;
                List<InstallEntry> events;
                var eventsManifest = new List<IntegrityRecord>();

                // Le 4 sorgenti sono indipendenti: in parallelo il tempo
                // totale e' quello della piu' lenta, non la somma.

                if (_sources.Current.IsLive)
                {
                    var wowTask = GetFromLocalMachineAsync(AppConstants.Registry.Wow6432UninstallPath, cancellationToken);
                    var msTask = GetFromLocalMachineAsync(AppConstants.Registry.MicrosoftUninstallPath, cancellationToken);
                    var usersTask = GetFromCurrentUserAsync(AppConstants.Registry.MicrosoftUninstallPath, cancellationToken);
                    var eventsTask = GetFromEventsAsync(eventsManifest, cancellationToken);

                    await Task.WhenAll(wowTask, msTask, usersTask, eventsTask);

                    wow6432Locals = await wowTask;
                    microsoftLocals = await msTask;
                    users = await usersTask;
                    events = await eventsTask;

                    manifest.Add(IntegrityRecord.LiveSource(EntryType.InstalledPrograms,
                        $@"HKLM\{AppConstants.Registry.Wow6432UninstallPath} (registro live, {wow6432Locals.Count} voci)"));
                    manifest.Add(IntegrityRecord.LiveSource(EntryType.InstalledPrograms,
                        $@"HKLM\{AppConstants.Registry.MicrosoftUninstallPath} (registro live, {microsoftLocals.Count} voci)"));
                    manifest.Add(IntegrityRecord.LiveSource(EntryType.InstalledPrograms,
                        $@"HKCU\{AppConstants.Registry.MicrosoftUninstallPath} (registro live, {users.Count} voci)"));
                    manifest.Add(IntegrityRecord.LiveSource(EntryType.InstalledPrograms,
                        $"Registro Applicazione (API live, {events.Count} voci)"));
                }
                else
                {
                    var offlineTask = Task.Run(() =>
                    {
                        // Un'unica lettura+parsing dell'hive SOFTWARE per
                        // entrambi i rami (198MB: dimezza I/O e CPU).
                        var software = GetUninstallFromHiveFile(_sources.Current.GetSoftwareHivePath(),
                            new[]
                            {
                                @"WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall",
                                @"Microsoft\Windows\CurrentVersion\Uninstall"
                            }, manifest, cancellationToken);
                        var usr = _sources.Current.GetUserHivePaths("NTUSER.DAT")
                            .SelectMany(h => GetUninstallFromHiveFile(h,
                                new[] { @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall" }, manifest, cancellationToken))
                            .ToList();
                        var ev = GetFromEvtxFile(
                            _sources.Current.GetEventLogPath(AppConstants.EventLog.ApplicationLog), eventsManifest, cancellationToken);
                        return (software, usr, ev);
                    }, cancellationToken);

                    await Task.WhenAll(offlineTask);

                    var offline = await offlineTask;
                    wow6432Locals = offline.software;
                    microsoftLocals = new List<InstallEntry>();
                    users = offline.usr;
                    events = offline.ev;

                    manifest.AddRange(eventsManifest);
                }

                var all = wow6432Locals.Concat(microsoftLocals).Concat(users).Concat(events).ToList();

                if (all.Count == 0)
                    return Result.Failure<List<InstallEntry>>("Nessuna sorgente programmi installati leggibile: " +
                        "chiavi Uninstall, log Applicazione e menu Start vuoti o inaccessibili.");

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

        private List<InstallEntry> GetFromEvtxFile(string evtxPath, List<IntegrityRecord> manifest, CancellationToken cancellationToken)
        {
            manifest.Add(IntegrityHasher.HashFile(evtxPath, EntryType.InstalledPrograms));

            if (!File.Exists(evtxPath))
                return new List<InstallEntry>();

            return BuildInstallEntriesFromEvents(Evidence.EvtxFileReader.ReadEvents(evtxPath), cancellationToken);
        }

        private Task<List<InstallEntry>> GetFromEventsAsync(List<IntegrityRecord> manifest, CancellationToken cancellationToken)
        {
            return Task.Run(() =>
            {
                manifest.Add(IntegrityHasher.HashFile(ApplicationLogPath, EntryType.InstalledPrograms));
                var events = Helpers.GetLogEntries(AppConstants.EventLog.ApplicationLog).ToList();
                return BuildInstallEntriesFromEvents(events, cancellationToken);
            }, cancellationToken);
        }

        private static List<InstallEntry> BuildInstallEntriesFromEvents(List<IEventRecord> events, CancellationToken cancellationToken)
        {
            var installedPrograms = events.Where(ev =>
                ev.EventId == AppConstants.EventLog.MsiInstallEventId &&
                ev.Source == AppConstants.EventLog.MsiInstallerProviderName).ToList();

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
                    DateBuilder.ToLocal(installedPrograms[i].TimeGenerated));

                if (entries.Any(ie => ie.FileName == entry.FileName &&
                    ie.InstallDate == entry.InstallDate)) continue;

                entries.Add(entry);
            }

            return entries;
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

        internal static List<InstallEntry> GetUninstallFromHiveFile(string hivePath, string[] relativePaths,
            List<IntegrityRecord> manifest, CancellationToken cancellationToken)
        {
            var entries = new List<InstallEntry>();

            manifest.Add(IntegrityHasher.HashFile(hivePath, EntryType.InstalledPrograms));

            byte[] bytes;
            try
            {
                bytes = File.ReadAllBytes(hivePath);
            }
            catch
            {
                return entries;
            }

            var reg = new Registry.RegistryHive(bytes, hivePath);
            _ = reg.ParseHive();

            foreach (var relativePath in relativePaths)
            {
                var key = GetKeyInsensitive(reg, relativePath);
                if (key == null) continue;

                foreach (var sub in key.SubKeys)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var entry = BuildInstallEntryFromHiveValues(sub, $"{hivePath}\\{sub.KeyPath}");
                    if (entry != null)
                        entries.Add(entry);
                }
            }

            return entries;
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

        private static InstallEntry BuildInstallEntryFromHiveValues(Registry.Abstractions.RegistryKey subKey, string dataSource)
        {
            var displayName = subKey.GetValue("DisplayName")?.ToString();

            if (!ShouldInclude(
                displayName,
                subKey.GetValue("SystemComponent")?.ToString(),
                subKey.GetValue("ParentKeyName")?.ToString(),
                subKey.GetValue("ReleaseType")?.ToString()))
            {
                return null;
            }

            return new InstallEntry(
                displayName,
                dataSource,
                subKey.GetValue("InstallLocation")?.ToString(),
                DateBuilder.BuildDateTimeFromString(subKey.GetValue("InstallDate")?.ToString()));
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
    }
}