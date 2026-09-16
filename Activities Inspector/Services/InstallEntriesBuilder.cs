using CSharpFunctionalExtensions;
using Microsoft.Win32;
using ProgettoInformaticaForense_Argentieri.Models;
using ProgettoInformaticaForense_Argentieri.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ProgettoInformaticaForense_Argentieri.Services
{
    public class InstallEntriesBuilder : IInstallEntriesBuilder
    {
        private const string WOW6432_KEY = @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall";
        private const string MICROSOFT_KEY = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";
        private const string LOG_FILTER = "Application";

        public async Task<Result<List<InstallEntry>>> GetInstallEntriesAsync()
        {
            try
            {
                var wow6432Locals = await GetFromLocalMachineAsync(WOW6432_KEY);
                var microsoftLocals = await GetFromLocalMachineAsync(MICROSOFT_KEY);
                var users = await GetFromCurrentUserAsync(MICROSOFT_KEY);
                var events = await GetFromEventsAsync();

                return Result.Success(wow6432Locals.Concat(microsoftLocals).Concat(users).Concat(events).ToList());
            }
            catch (Exception ex)
            {
                return Result.Failure<List<InstallEntry>>(ex.Message);
            }
        }

        private async Task<IEnumerable<InstallEntry>> GetFromLocalMachineAsync(string key)
        {
            return await Task.Run(() =>
            {
                var entries = new List<InstallEntry>();

                using (var rk = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(key))
                {
                    foreach (var skName in rk.GetSubKeyNames())
                    {
                        using (var sk = rk.OpenSubKey(skName))
                        {
                            entries.Add(BuildInstallEntry(sk));
                        }
                    }
                }

                return entries;
            });
        }

        private async Task<IEnumerable<InstallEntry>> GetFromCurrentUserAsync(string key)
        {
            return await Task.Run(() =>
            {
                var entries = new List<InstallEntry>();

                using (var rk = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(key))
                {
                    foreach(var skName in rk.GetSubKeyNames())
                    {
                        using (var sk = rk.OpenSubKey(skName))
                        {
                            entries.Add(BuildInstallEntry(sk));
                        }
                    }
                }

                return entries;
            });
        }

        private async static Task<IEnumerable<InstallEntry>> GetFromEventsAsync()
        {
            return await Task.Run(() =>
            {
                var events = Utility.Helpers.GetLogEntries(LOG_FILTER);

                var installedPrograms = events.Where(ev => ev.EventID == 11707).ToList();

                var entries = new List<InstallEntry>();

                for (int i = 0; i < installedPrograms.Count; i++)
                {
                    var substrings = installedPrograms[i].ReplacementStrings[0].Split(':');
                    var substrings2 = substrings[1].Split(new char[] { '-', '-' });
                    var fileName = substrings2[0].Trim();

                    var entry = new InstallEntry(fileName, string.Empty, string.Empty, 
                        installedPrograms[i].TimeGenerated.ToLocalTime());

                    if (entries.Any(ie => ie.FileName == entry.FileName && 
                        ie.InstallDate == entry.InstallDate)) continue;
                    entries.Add(entry);
                }

                return entries;
            });
        }

        private InstallEntry BuildInstallEntry(RegistryKey registryKey)
            => new InstallEntry(registryKey.GetValue("DisplayName")?.ToString(), registryKey.ToString(),
                registryKey.GetValue("InstallLocation")?.ToString(),
                DateBuilder.BuildDateTimeFromString(registryKey.GetValue("InstallDate")?.ToString()));
    }
}
