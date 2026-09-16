using CSharpFunctionalExtensions;
using ProgettoInformaticaForense_Argentieri.Constants;
using ProgettoInformaticaForense_Argentieri.Models;
using ProgettoInformaticaForense_Argentieri.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ProgettoInformaticaForense_Argentieri.Services
{
    public class LoggedInfoService : ILoggedInfoService
    {
        public async Task<Result<List<SessionEntry>>> GetSessionsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var sessionsList = new List<SessionEntry>();

                var systemEvents = await GetSecurityEventLogEntriesAsync(cancellationToken);

                var logOnEntries = GetLogOnEntries(systemEvents).ToList();
                var logOffEntries = GetLogOffEntries(systemEvents).ToList();

                foreach (var logOffEntry in logOffEntries)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var selectedLogOnEntry = logOnEntries
                        .FirstOrDefault(ev => ev.Index == logOffEntry.Index);

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
                    cancellationToken.ThrowIfCancellationRequested();

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
            catch (Exception ex) when (!(ex is OperationCanceledException))
            {
                return Result.Failure<List<SessionEntry>>(ex.ToString());
            }
        }

        private Task<List<EventLogEntry>> GetSecurityEventLogEntriesAsync(CancellationToken cancellationToken = default)
        {
            return Task.Run(() =>
            {
                using var eventLog = new EventLog { Log = AppConstants.EventLog.SecurityLog };
                var entries = new List<EventLogEntry>();

                foreach (EventLogEntry entry in eventLog.Entries)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    entries.Add(entry);
                }

                return entries;
            }, cancellationToken);
        }

        private IEnumerable<LogOnEntry> GetLogOnEntries(List<EventLogEntry> systemEvents)
        {
            var logOnEntries = systemEvents.Where(ev => ev.EventID == AppConstants.EventLog.LogonEventId).ToList();

            var filteredByAccessType = FilterByAccessType(logOnEntries).ToList();

            var filteredByAccountName = filteredByAccessType.Where(ev => 
                !ev.ReplacementStrings[5].StartsWith("UMFD-") &&
                !ev.ReplacementStrings[5].StartsWith("DWM-")).ToList();

            var entries = BuildLogOnEntries(filteredByAccountName).ToList();
            var distinctEntries = RemoveDuplicates(entries).ToList();

            return distinctEntries;
        }

        private IEnumerable<LogoffEntry> GetLogOffEntries(List<EventLogEntry> systemEvents)
        {
            var logOffEntries = systemEvents.Where(ev => ev.EventID == AppConstants.EventLog.LogoffEventId).ToList();

            foreach (var entry in logOffEntries)
            {
                yield return new LogoffEntry(entry.ReplacementStrings[3], entry.TimeGenerated);
            }
        }

        private static IEnumerable<EventLogEntry> FilterByAccessType(IEnumerable<EventLogEntry> events)
        {
            return events.Where(ev =>
                ev.ReplacementStrings[8] != "0" &&
                ev.ReplacementStrings[8] != "3" &&
                ev.ReplacementStrings[8] != "5" &&
                ev.ReplacementStrings[8] != "7");
        }

        private static IEnumerable<LogOnEntry> BuildLogOnEntries(List<EventLogEntry> entries)
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

        private static IEnumerable<LogOnEntry> RemoveDuplicates(List<LogOnEntry> entries)
        {
            var distinctEntries = new List<LogOnEntry>();

            for (var i = 0; i < entries.Count; i++)
            {
                if (i == entries.Count - 1)
                {
                    distinctEntries.Add(entries[i]);
                    continue;
                }

                if (!entries[i].Equals(entries[i + 1]))
                {
                    distinctEntries.Add(entries[i]);
                }
            }

            return distinctEntries;
        }
    }
}