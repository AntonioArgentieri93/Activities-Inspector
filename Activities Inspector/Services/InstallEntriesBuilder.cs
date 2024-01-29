using CSharpFunctionalExtensions;
using Microsoft.Win32;
using ProgettoInformaticaForense_Argentieri.Models;
using ProgettoInformaticaForense_Argentieri.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace ProgettoInformaticaForense_Argentieri.Services
{
    public class InstallEntriesBuilder : IInstallEntriesBuilder
    {
        private const string WOW6432_KEY = @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall";
        private const string MICROSOFT_KEY = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";

        public async Task<Result<List<InstallEntry>>> GetInstallEntries()
        {
            var taskCompletionSource = new TaskCompletionSource<Result<List<InstallEntry>>>(
                TaskCreationOptions.RunContinuationsAsynchronously);

            try
            {
                await Task.Run(() =>
                {
                    var wow6432Locals = GetFromLocalMachine(WOW6432_KEY);
                    var microsoftLocals = GetFromLocalMachine(MICROSOFT_KEY);
                    var users = GetFromCurrentUser(MICROSOFT_KEY);
                    var events = GetFromEvents();

                    taskCompletionSource.SetResult(Result.Success((wow6432Locals.Concat(microsoftLocals).Concat(users).Concat(events).ToList())));
                });
            }
            catch (Exception ex)
            {
                taskCompletionSource.SetResult(Result.Failure<List<InstallEntry>>(ex.Message));
            }

            return taskCompletionSource.Task.Result;
        }

        private IEnumerable<InstallEntry> GetFromLocalMachine(string key)
        {
            using (RegistryKey rk = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(key))
            {
                if (rk == null) yield break;

                foreach (string skName in rk.GetSubKeyNames())
                {
                    using (RegistryKey sk = rk.OpenSubKey(skName))
                    {
                        yield return BuildInstallEntry(sk);
                    }
                }
            }
        }

        private IEnumerable<InstallEntry> GetFromCurrentUser(string key)
        {
            using (RegistryKey rk = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(key))
            {
                if (rk == null) yield break;

                foreach (string skName in rk.GetSubKeyNames())
                {
                    using (RegistryKey sk = rk.OpenSubKey(skName))
                    {
                        yield return BuildInstallEntry(sk);
                    }
                }
            }
        }

        private static IEnumerable<InstallEntry> GetFromEvents()
        {
            var events = GetSystemEvents();

            var installedPrograms = events.Where(ev => ev.EventID == 11707).ToList();

            var entries = new List<InstallEntry>();

            for (int i = 0; i < installedPrograms.Count; i++)
            {
                var substrings = installedPrograms[i].ReplacementStrings[0].Split(':');
                var substrings2 = substrings[1].Split(new char[] { '-', '-' });
                var fileName = substrings2[0].Trim();

                var entry = new InstallEntry(fileName, string.Empty, string.Empty, installedPrograms[i].TimeGenerated.ToLocalTime());

                if (entries.Any(ie => ie.FileName == entry.FileName && ie.InstallDate == entry.InstallDate)) continue;
                entries.Add(entry);
            }

            return entries;
        }

        private static IEnumerable<EventLogEntry> GetSystemEvents()
        {
            var myLog = new EventLog();
            myLog.Log = "Application";

            foreach (var @event in myLog.Entries)
            {
                var logEntry = (EventLogEntry)@event;
                yield return logEntry;
            }
        }

        private InstallEntry BuildInstallEntry(RegistryKey registryKey)
        {
            return new InstallEntry(registryKey.GetValue("DisplayName")?.ToString(), registryKey.ToString(),
                registryKey.GetValue("InstallLocation")?.ToString(), 
                DateBuilder.BuildDateTimeFromString(registryKey.GetValue("InstallDate")?.ToString()));
        }
    }
}
