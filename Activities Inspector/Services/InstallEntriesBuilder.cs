using CSharpFunctionalExtensions;
using Microsoft.Win32;
using Activities_Inspector.Constants;
using Activities_Inspector.Models;
using Activities_Inspector.Utils;
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

                return Result.Success(wow6432Locals.Concat(microsoftLocals).Concat(users).Concat(events).ToList());
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
                    if (sk != null)
                        entries.Add(BuildInstallEntry(sk));
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
                    if (sk != null)
                        entries.Add(BuildInstallEntry(sk));
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

                    var substrings = installedPrograms[i].ReplacementStrings[0].Split(':');
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
            => new InstallEntry(
                registryKey.GetValue("DisplayName")?.ToString(),
                registryKey.ToString(),
                registryKey.GetValue("InstallLocation")?.ToString(),
                DateBuilder.BuildDateTimeFromString(registryKey.GetValue("InstallDate")?.ToString()));
    }
}