using CSharpFunctionalExtensions;
using ProgettoInformaticaForense_Argentieri.Constants;
using ProgettoInformaticaForense_Argentieri.Models;
using ProgettoInformaticaForense_Argentieri.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ProgettoInformaticaForense_Argentieri.Services
{
    public class SystemTimeChangedService : ISystemTimeChangedService
    {
        public async Task<Result<List<SystemTimeChangedEntry>>> GetSystemTimeChangedEntriesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var entries = new List<SystemTimeChangedEntry>();

                var logEntries = await GetSystemTimeChangedEventLogEntriesAsync(cancellationToken);

                foreach (var entry in logEntries)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (entry.ReplacementStrings.Length < 8) continue;

                    if (entry.ReplacementStrings[1] == "LOCAL SERVICE" ||
                        entry.ReplacementStrings[1] == "SERVIZIO LOCALE") continue;

                    if (entry.ReplacementStrings[7] == @"C:\Windows\System32\svchost.exe") continue;

                    if (!DateTime.TryParseExact(entry.TimeGenerated.ToString("dd/M/yyyy HH:mm:ss"), "dd/M/yyyy HH:mm:ss",
                        DateTimeFormatInfo.InvariantInfo, DateTimeStyles.None, out DateTime timeGenerated))
                    {
                        continue;
                    }

                    if (!DateTime.TryParse(entry.ReplacementStrings[4], null, DateTimeStyles.RoundtripKind, out DateTime oldTime) ||
                        !DateTime.TryParse(entry.ReplacementStrings[5], null, DateTimeStyles.RoundtripKind, out DateTime newTime))
                    {
                        continue;
                    }

                    oldTime = oldTime.ToLocalTime();
                    newTime = newTime.ToLocalTime();

                    if (oldTime == newTime) continue;

                    entries.Add(new SystemTimeChangedEntry(entry.ReplacementStrings[1],
                        DateBuilder.BuildFromDateTime(timeGenerated),
                        DateBuilder.BuildFromString(oldTime.ToString()),
                        DateBuilder.BuildFromString(newTime.ToString())));
                }

                entries = entries.OrderBy(ee => ee.TimeGenerated).ToList();

                return Result.Success(entries);
            }
            catch (Exception ex) when (!(ex is OperationCanceledException))
            {
                return Result.Failure<List<SystemTimeChangedEntry>>(ex.ToString());
            }
        }

        private async Task<List<EventLogEntry>> GetSystemTimeChangedEventLogEntriesAsync(CancellationToken cancellationToken = default)
        {
            return await Task.Run(() =>
            {
                using var eventLog = new EventLog { Log = AppConstants.EventLog.SecurityLog };
                var entries = new List<EventLogEntry>();

                foreach (EventLogEntry entry in eventLog.Entries)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (entry.EventID == AppConstants.EventLog.SystemTimeChangedEventId)
                        entries.Add(entry);
                }

                return entries;
            }, cancellationToken);
        }
    }
}