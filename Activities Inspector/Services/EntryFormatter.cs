using Activities_Inspector.Models;
using Activities_Inspector.Utils;
using System;

namespace Activities_Inspector.Services
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
            string endInterval = string.Empty;

            if (usageInfo.Interval.End.HasValue)
            {
                endInterval = DateBuilder.BuildFromDateTime(usageInfo.Interval.End.Value);
            }

            var duration = GetDuration(usageInfo.Duration);

            return string.Format(
                "{0} ; {1} ; {2} ; {3} ; {4}",
                DateBuilder.BuildFromDateTime(usageInfo.Interval.Start),
                endInterval,
                duration,
                usageInfo.MachineName,
                usageInfo.Interval.StartedAfterCrash ? "Sì" : "No");
        }

        private string BuildInstallEntryFormat(InstallEntry installEntry)
            => string.Format(
                "{0} ; {1} ; {2} ; {3}",
                installEntry.FileName,
                installEntry.DataSource,
                installEntry.FullPath,
                installEntry.InstallDate.HasValue ? DateBuilder.BuildFromDateTime(installEntry.InstallDate.Value) : string.Empty);

        private string BuildRecentFolderEntryFormat(RecentFolderEntry recentFolderEntry)
            => string.Format(
                "{0} ; {1} ; {2} ; {3} ; {4}",
                recentFolderEntry.FileName,
                recentFolderEntry.DataSource,
                recentFolderEntry.FullPath,
                DateBuilder.BuildFromDateTime(recentFolderEntry.ActionTime),
                recentFolderEntry.SkippedShellItems);

        private string BuildPrefetchInfoEntryFormat(PrefetchInfoEntry prefetchInfoEntry)
            => string.Format(
                "{0} ; {1} ; {2} ; {3} ; {4} ; {5}",
                prefetchInfoEntry.ExecutableFileName,
                prefetchInfoEntry.SourceFileName,
                DateBuilder.BuildFromDateTime(prefetchInfoEntry.LastRunTime),
                prefetchInfoEntry.Extension,
                DateBuilder.BuildFromDateTime(prefetchInfoEntry.FirstRunTime),
                prefetchInfoEntry.RunCount);

        private string BuildShellBagEntry(ShellBagEntry shellBagEntry)
            => string.Format(
                "{0} ; {1} ; {2}",
                shellBagEntry.AbsolutePath,
                DateBuilder.BuildFromDateTime(shellBagEntry.LastRegistryWriteDate),
                shellBagEntry.RegistryPath);

        private string BuildSessionEntry(SessionEntry sessionEntry)
        {
            var logOffTime = string.Empty;
            var duration = string.Empty;

            if (sessionEntry.LogOffTime.HasValue)
            {
                logOffTime = DateBuilder.BuildFromDateTime(sessionEntry.LogOffTime.Value);
            }

            if (sessionEntry.Duration.HasValue)
            {
                duration = GetDuration(sessionEntry.Duration.Value);
            }

            var accessType = AccessTypeBuilder.BuildStringSessionType(sessionEntry.AccessType);

            return string.Format(
                "{0} ; {1} ; {2} ; {3} ; {4} ; {5} ; {6} ; {7} ; {8} ; {9}",
                sessionEntry.UserName,
                sessionEntry.Group,
                sessionEntry.MachineName,
                DateBuilder.BuildFromDateTime(sessionEntry.LogOnTime),
                logOffTime,
                duration,
                sessionEntry.NetworkAddress,
                accessType,
                sessionEntry.Note ?? string.Empty,
                sessionEntry.Index ?? string.Empty);
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
