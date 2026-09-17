using CSharpFunctionalExtensions;
using Microsoft.Win32;
using Activities_Inspector.Constants;
using Activities_Inspector.Models;
using Activities_Inspector.Utils;
using System;
using System.Collections.Generic;
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

                var all = wow6432Locals.Concat(microsoftLocals).Concat(users).Concat(events).ToList();
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
                var installedPrograms = events.Where(ev => ev.InstanceId == 11707).ToList();

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
                .Select(g => g.OrderByDescending(e => e.InstallDate.HasValue).First())
                .ToList();
        }
    }
}