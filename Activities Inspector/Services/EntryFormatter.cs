using ProgettoInformaticaForense_Argentieri.Models;
using ProgettoInformaticaForense_Argentieri.Utils;
using System;

namespace ProgettoInformaticaForense_Argentieri.Services
{
    public class EntryFormatter : IEntryFormatter
    {
        public string AsCsv(Entry entry)
        {
            if (entry == null) throw new ArgumentNullException(nameof(entry));

            if (entry is UsageInfo)
            {
                var usageInfo = (UsageInfo)entry;
                return BuildUsageInfoFormat(usageInfo);
            }

            if(entry is InstallEntry)
            {
                var installEntry = (InstallEntry)entry;
                return BuildInstallEntryFormat(installEntry);
            }

            if(entry is RecentFolderEntry)
            {
                var recentFolderEntry = (RecentFolderEntry)entry;
                return BuildRecentFolderEntryFormat (recentFolderEntry);
            }

            if(entry is PrefetchInfoEntry)
            {
                var prefetchInfoEntry = (PrefetchInfoEntry)entry;
                return BuildPrefetchInfoEntryFormat (prefetchInfoEntry);
            }

            if (entry is ShellBagEntry)
            {
                var shellBagEntry = (ShellBagEntry)entry;
                return BuildShellBagEntry(shellBagEntry);
            }

            if (entry is SessionEntry)
            {
                var sessionEntry = (SessionEntry)entry;
                return BuildSessionEntry(sessionEntry);
            }

            if(entry is SystemTimeChangedEntry)
            {
                var systemTimeChangedEntry = (SystemTimeChangedEntry)entry;
                return BuildSystemTimeChangedEntry(systemTimeChangedEntry);
            }

            if(entry is UsbEntry)
            {
                var usbEntry = (UsbEntry)entry;
                return BuildUsbEntry(usbEntry);
            }

            else throw new ArgumentException(nameof(entry));
        }

        private string BuildUsageInfoFormat(UsageInfo usageInfo)
        {
            var offset = TimeZoneInfo.Local.GetUtcOffset(DateTime.UtcNow);
            string endInterval = string.Empty;

            if (usageInfo.Interval.End.HasValue)
            {
                endInterval = usageInfo.Interval.End.Value.ToString("dd/M/yyyy HH:mm:ss") + $"GMT+{offset.Hours}";
            }

            var duration = GetDuration(usageInfo.Duration);

            return string.Format(
                "{0} ; {1} ; {2} ; {3}",
                usageInfo.Interval.Start.ToString("dd/M/yyyy HH:mm:ss") + $"GMT+{offset.Hours}",
                endInterval,
                duration,
                usageInfo.MachineName);
        }

        private string BuildInstallEntryFormat(InstallEntry installEntry)
            => string.Format(
                "{0} ; {1} ; {2} ; {3}",
                installEntry.FileName,
                installEntry.DataSource,
                installEntry.FullPath,
                installEntry.InstallDate?.ToShortDateString());

        private string BuildRecentFolderEntryFormat(RecentFolderEntry recentFolderEntry)
            => string.Format(
                "{0} ; {1} ; {2} ; {3}",
                recentFolderEntry.FileName,
                recentFolderEntry.DataSource,
                recentFolderEntry.FullPath,
                DateBuilder.BuildFromDateTime(recentFolderEntry.ActionTime));

        private string BuildPrefetchInfoEntryFormat(PrefetchInfoEntry prefetchInfoEntry)
            => string.Format(
                "{0} ; {1} ; {2} ; {3}",
                prefetchInfoEntry.ExecutableFileName,
                prefetchInfoEntry.SourceFileName,
                DateBuilder.BuildFromDateTime(prefetchInfoEntry.LastRunTime),
                prefetchInfoEntry.Extension);

        private string BuildShellBagEntry(ShellBagEntry shellBagEntry)
            => string.Format(
                "{0} ; {1} ; {2}",
                shellBagEntry.AbsolutePath,
                DateBuilder.BuildFromDateTime(shellBagEntry.LastRegistryWriteDate),
                shellBagEntry.RegistryPath);

        private string BuildSessionEntry(SessionEntry sessionEntry)
        {
            var offset = TimeZoneInfo.Local.GetUtcOffset(DateTime.UtcNow);
            var logOffTime = string.Empty;
            var duration = string.Empty;

            if (sessionEntry.LogOffTime.HasValue)
            {
                logOffTime = sessionEntry.LogOffTime.Value.ToString("dd/M/yyyy HH:mm:ss") + $"GMT+{offset.Hours}";
            }

            if (sessionEntry.Duration.HasValue)
            {
                duration = GetDuration(sessionEntry.Duration.Value);
            }

            var accessType = AccessTypeBuilder.BuildStringSessionType(sessionEntry.AccessType);

            return string.Format(
                "{0} ; {1} ; {2} ; {3} ; {4} ; {5} ; {6} ; {7}",
                sessionEntry.UserName,
                sessionEntry.Group,
                sessionEntry.MachineName,
                sessionEntry.LogOnTime,
                logOffTime,
                duration,
                sessionEntry.NetworkAddress,
                accessType);
        }

        private string BuildSystemTimeChangedEntry(SystemTimeChangedEntry systemTimeChangedEntry)
            => string.Format(
                "{0} ; {1} ; {2} ; {3}",
                systemTimeChangedEntry.AccountName,
                systemTimeChangedEntry.TimeGenerated,
                systemTimeChangedEntry.OldTime,
                systemTimeChangedEntry.NewTime);

        private string BuildUsbEntry(UsbEntry usbEntry)
         => string.Format("{0} ; {1} ; {2} ; {3} ; {4} ; {5} ; {6} ; {7}",
                MapUsbState(usbEntry.Plugged),
                usbEntry.DeviceName,
                usbEntry.SerialNumber,
                usbEntry.VendorId,
                usbEntry.ProductId,
                usbEntry.UsbClass,
                DateBuilder.BuildFromDateTimeOffset(usbEntry.LastConnected),
                DateBuilder.BuildFromDateTimeOffset(usbEntry.LastRemoved));

        private string GetDuration(TimeSpan duration)
        {
            if (duration == null) return string.Empty;

            return $"{duration.Days} giorno/i - {duration.Hours} ore - {duration.Minutes} minuti - " +
                $"{duration.Seconds} secondi.";
        }

        private string MapUsbState(bool plugged)
            => plugged ? "Connesso" : "Non connesso";
    }
}
