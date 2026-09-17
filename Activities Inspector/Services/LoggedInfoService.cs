using CSharpFunctionalExtensions;
using Activities_Inspector.Constants;
using Activities_Inspector.Models;
using Activities_Inspector.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Activities_Inspector.Services
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
            var logOnEntries = systemEvents.Where(ev => ev.InstanceId == AppConstants.EventLog.LogonEventId).ToList();

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
            var logOffEntries = systemEvents.Where(ev => ev.InstanceId == AppConstants.EventLog.LogoffEventId).ToList();

            foreach (var entry in logOffEntries)
            {
                yield return new LogoffEntry(entry.ReplacementStrings[3], entry.TimeGenerated);
            }
        }

        private static readonly int[] HumanAccessTypes = { 2, 7, 9, 10, 11 };

        private static IEnumerable<EventLogEntry> FilterByAccessType(IEnumerable<EventLogEntry> events)
        {
            // Allow-list dei tipi guidati da persona: 2 interattivo, 7 sblocco,
            // 9 nuove credenziali, 10 remoto, 11 cached. Fuori restano sistema e
            // rete (0 sistema, 3 rete, 4 batch, 5 servizio, 8 cleartext).
            return events.Where(ev =>
                int.TryParse(ev.ReplacementStrings[8], out int accessType) &&
                HumanAccessTypes.Contains(accessType));
        }

        private static IEnumerable<LogOnEntry> BuildLogOnEntries(List<EventLogEntry> entries)
        {
            foreach (var entry in entries)
            {
                LogOnEntry parsed = null;

                try
                {
                    parsed = new LogOnEntry(
                        eventId: (int)entry.InstanceId,
                        machineName: entry.MachineName,
                        index: entry.ReplacementStrings[7],
                        timeGenerated: entry.TimeGenerated,
                        accountName: entry.ReplacementStrings[5],
                        domainName: entry.ReplacementStrings[6],
                        group: entry.ReplacementStrings[2],
                        accessType: Convert.ToInt32(entry.ReplacementStrings[8]),
                        sourceAddress: entry.ReplacementStrings[18]);
                }
                catch
                {
                    continue;
                }

                yield return parsed;
            }
        }

        private static IEnumerable<LogOnEntry> RemoveDuplicates(List<LogOnEntry> entries)
        {
            if (entries == null) throw new ArgumentNullException(nameof(entries));

            // Distinct preserva l'ordine di prima occorrenza e, grazie a
            // Equals/GetHashCode basati sul Logon ID, rimuove i duplicati
            // ovunque si trovino (non solo se adiacenti).
            return entries.Distinct().ToList();
        }
    }
}