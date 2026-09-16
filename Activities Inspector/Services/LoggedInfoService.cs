using CSharpFunctionalExtensions;
using ProgettoInformaticaForense_Argentieri.Models;
using ProgettoInformaticaForense_Argentieri.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace ProgettoInformaticaForense_Argentieri.Services
{
    public class LoggedInfoService : ILoggedInfoService
    {
        private const string LOG_FILTER = "Security";

        public async Task<Result<List<SessionEntry>>> GetSessionsAsync()
        {
            return await Task.Run(async () =>
            {
                try
                {
                    var sessionsList = new List<SessionEntry>();

                    var systemEvents = Helpers.GetLogEntries(LOG_FILTER).ToList();

                    var logOnTask = GetLogOnEntriesAsync(systemEvents);
                    var logOffTask = GetLogOffEntriesAsync(systemEvents);
                    var logOnEntries = (await logOnTask).ToList();
                    var logOffEntries = (await logOffTask).ToList();

                    foreach (var logOffEntry in logOffEntries)
                    {
                        var selectedLogOnEntry = logOnEntries
                            .Where(ev => ev.Index == logOffEntry.Index)
                            .FirstOrDefault();

                        if (selectedLogOnEntry != null)
                        {
                            sessionsList.Add(new SessionEntry(
                                index: selectedLogOnEntry.Index,
                                userName: selectedLogOnEntry.AccountName,
                                group: selectedLogOnEntry.DomainName,
                                machineName: selectedLogOnEntry.MachinName,
                                logOnTime: selectedLogOnEntry.TimeGenerated,
                                logOffTime: logOffEntry.TimeGenerated,
                                duration: logOffEntry.TimeGenerated.Subtract(selectedLogOnEntry.TimeGenerated),
                                networdAddress: selectedLogOnEntry.SourceAddress,
                                accessType: selectedLogOnEntry.AccessType.ToString()));

                            logOnEntries.Remove(selectedLogOnEntry);
                        }
                    }

                    foreach (var logOnEntry in logOnEntries)
                    {
                        sessionsList.Add(new SessionEntry(
                            index: logOnEntry.Index,
                            userName: logOnEntry.AccountName,
                            group: logOnEntry.DomainName,
                            machineName: logOnEntry.MachinName,
                            logOnTime: logOnEntry.TimeGenerated,
                            logOffTime: null,
                            duration: null,
                            networdAddress: logOnEntry.SourceAddress,
                            accessType: logOnEntry.AccessType.ToString()));
                    }

                    sessionsList = sessionsList.OrderBy(ev => ev.LogOnTime).ToList();

                    return Result.Success(sessionsList);
                }
                catch (Exception ex)
                {
                    return Result.Failure<List<SessionEntry>>(ex.Message);
                }
            });
        }

        private async Task<IEnumerable<LogOnEntry>> GetLogOnEntriesAsync(List<EventLogEntry> systemEvents)
        {
            return await Task.Run(() =>
            {
                var logOnEntries = systemEvents.Where(ev => ev.EventID == 4624).ToList();

                //Filtro evento
                var filteredByAccessType = FilterByAccessType(logOnEntries).ToList();

                //Filtro nome utente
                var filteredByAccountName = filteredByAccessType.Where(ev => ev.ReplacementStrings[5].StartsWith("UMFD-") == false &&
                    ev.ReplacementStrings[5].StartsWith("DWM-") == false).ToList();

                //Filtro duplicati
                var entries = BuildLogOnEntries(filteredByAccountName).ToList();
                var distinctEntries = RemoveDuplicates(entries).ToList();

                return distinctEntries;
            });
        }

        private async Task<IEnumerable<LogoffEntry>> GetLogOffEntriesAsync(List<EventLogEntry> systemEvents)
        {
            return await Task.Run(() =>
            {
                var entries = new List<LogoffEntry>();

                var logOffEntries = systemEvents.Where(ev => ev.EventID == 4647).ToList();

                foreach(var entry in logOffEntries)
                {
                    entries.Add(new LogoffEntry(entry.ReplacementStrings[3], entry.TimeGenerated));
                }

                return entries; 
            });
        }

        private IEnumerable<EventLogEntry> FilterByAccessType(IEnumerable<EventLogEntry> events)
        {
            var filteredEvents = events.Where(ev => 
                ev.ReplacementStrings[8] != "0" &&
                ev.ReplacementStrings[8] != "3" &&
                ev.ReplacementStrings[8] != "5" &&
                ev.ReplacementStrings[8] != "7");

            foreach (var item in filteredEvents)
                yield return item;
        }

        private IEnumerable<LogOnEntry> BuildLogOnEntries(List<EventLogEntry> entries)
        {
            foreach (var entry in entries)
            {
                yield return new LogOnEntry(
                    eventId: entry.EventID,
                    machineName: entry.MachineName,
                    index: entry.ReplacementStrings[7],
                    timeGenerated: entry.TimeGenerated,
                    accountName: entry.ReplacementStrings[5],
                    domainName: entry.ReplacementStrings[6],
                    group: entry.ReplacementStrings[2],
                    accessType: Convert.ToInt32(entry.ReplacementStrings[8]),
                    sourceAddress: entry.ReplacementStrings[18]);
            }
        }

        private IEnumerable<LogOnEntry> RemoveDuplicates(List<LogOnEntry> entries)
        {
            var distinctEntries = new List<LogOnEntry>();

            for (var i = 0; i < entries.Count; i++)
            {
                if (i == entries.Count - 1) continue;

                if (entries[i].Equals(entries[i + 1]))
                {
                    entries.RemoveAt(i);
                }
            }

            distinctEntries = entries;

            return distinctEntries;
        }
    }
}
