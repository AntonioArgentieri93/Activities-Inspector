using CSharpFunctionalExtensions;
using ProgettoInformaticaForense_Argentieri.Models;
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
            var taskCompletionSource = new TaskCompletionSource<Result<List<SessionEntry>>>(
                TaskCreationOptions.RunContinuationsAsynchronously);

            var sessionsList = new List<SessionEntry>();

            try
            {
                await Task.Run(() =>
                {
                    var logOnEntries = GetLogOnEntries().ToList();
                    var logOffEntries = GetLogOffEntries().ToList();

                    foreach (var logOffEntry in logOffEntries)
                    {
                        var selectedLogOnEntry = logOnEntries.Where(ev => ev.Index == logOffEntry.Index).FirstOrDefault();

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

                    sessionsList.OrderBy(ev => ev.LogOnTime);
                });

                taskCompletionSource.SetResult(Result.Success(sessionsList));
            }
            catch (Exception ex)
            {
                taskCompletionSource.SetResult(Result.Failure<List<SessionEntry>>(ex.Message));
            }

            return taskCompletionSource.Task.Result;
        }

        private IEnumerable<LogOnEntry> GetLogOnEntries()
        {
            var systemEvents = GetSystemEvents().ToList();

            var logOnEntries = systemEvents.Where(ev => ev.EventID == 4624).ToList();

            //Filtro evento
            var filteredByAccessType = FilterByAccessType(logOnEntries).ToList();

            //Filtro nome utente
            var filteredByAccountName = filteredByAccessType.Where(ev => ev.ReplacementStrings[5].StartsWith("UMFD-") == false &&
                ev.ReplacementStrings[5].StartsWith("DWM-") == false).ToList();

            //Filtro duplicati
            var entries = BuildLogOnEntries(filteredByAccountName).ToList();
            var distinctEntries = RemoveDuplicates(entries).ToList();

            foreach (var entry in distinctEntries)
                yield return entry;
        }

        private IEnumerable<LogoffEntry> GetLogOffEntries()
        {
            var systemEvents = GetSystemEvents().ToList();

            var logOffEntries = systemEvents.Where(ev => ev.EventID == 4647).ToList();

            foreach (var entry in logOffEntries)
                yield return new LogoffEntry(entry.ReplacementStrings[3], entry.TimeGenerated);
        }

        private IEnumerable<EventLogEntry> GetSystemEvents()
        {
            var myLog = new EventLog();
            myLog.Log = LOG_FILTER;

            foreach (var @event in myLog.Entries)
            {
                var logEntry = (EventLogEntry)@event;
                yield return logEntry;
            }
        }

        private IEnumerable<EventLogEntry> FilterByAccessType(IEnumerable<EventLogEntry> events)
        {
            var noCat0 = events.Where(ev => ev.ReplacementStrings[8] != "0");
            var noCat3 = noCat0.Where(ev => ev.ReplacementStrings[8] != "3");
            var noCat5 = noCat3.Where(ev => ev.ReplacementStrings[8] != "5");
            var noCat7 = noCat5.Where(ev => ev.ReplacementStrings[8] != "7");

            foreach (var item in noCat7)
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
