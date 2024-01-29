using CSharpFunctionalExtensions;
using ProgettoInformaticaForense_Argentieri.Models;
using ProgettoInformaticaForense_Argentieri.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace ProgettoInformaticaForense_Argentieri.Services
{
    public class SystemTimeChangedService : ISystemTimeChangedService
    {
        private const string LOG_FILTER = "Security";

        public async Task<Result<List<SystemTimeChangedEntry>>> GetSystemTimeChangedEntriesAsync()
        {
            var taskCompletionSource = new TaskCompletionSource<Result<List<SystemTimeChangedEntry>>>(
                TaskCreationOptions.RunContinuationsAsynchronously);

            var entries = new List<SystemTimeChangedEntry>();

            try
            {
                await Task.Run(() =>
                {
                    var logEntries = GetSystemTimeChangedEventLogEntries().ToList();

                    foreach (var entry in logEntries)
                    {
                        if (entry.ReplacementStrings[1] == "LOCAL SERVICE" || 
                            entry.ReplacementStrings[1] == "SERVIZIO LOCALE") continue;

                        if (entry.ReplacementStrings[7] == @"C:\Windows\System32\svchost.exe") continue;

                        DateTime timeGenerated;
                        DateTime.TryParseExact(entry.TimeGenerated.ToString(), "dd/M/yyyy HH:mm:ss",
                            DateTimeFormatInfo.InvariantInfo, DateTimeStyles.None, out timeGenerated);

                        DateTime oldTime = DateTime.Parse(entry.ReplacementStrings[4], null, 
                            DateTimeStyles.RoundtripKind).ToLocalTime();
                        DateTime newTime = DateTime.Parse(entry.ReplacementStrings[5], null, 
                            DateTimeStyles.RoundtripKind).ToLocalTime();

                        if (oldTime.ToString().Equals(newTime.ToString())) continue;

                        entries.Add(new SystemTimeChangedEntry(entry.ReplacementStrings[1], DateBuilder.BuildFromDateTime(timeGenerated),
                            DateBuilder.BuildFromString(oldTime.ToString()), DateBuilder.BuildFromString(newTime.ToString())));
                    }
                });

                entries.OrderBy(ee => ee.TimeGenerated);

                taskCompletionSource.SetResult(Result.Success(entries));
            }
            catch (Exception ex)
            {
                taskCompletionSource.SetResult(Result.Failure<List<SystemTimeChangedEntry>>(ex.Message));
            }

            return taskCompletionSource.Task.Result;
        }

        private IEnumerable<EventLogEntry> GetSystemTimeChangedEventLogEntries()
        {
            var systemEvents = GetSystemEvents().ToList();

            return systemEvents.Where(ev => ev.EventID == 4616);
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
    }
}
