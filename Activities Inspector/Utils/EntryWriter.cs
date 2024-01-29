using ProgettoInformaticaForense_Argentieri.Models;
using ProgettoInformaticaForense_Argentieri.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ProgettoInformaticaForense_Argentieri.Utility
{
    public class EntryWriter : StreamWriter
    {
        private readonly IEntryFormatter _entryFormatter;

        public EntryWriter(string path, bool append, Encoding encoding, IEntryFormatter entryFormatter) : base(path, append, encoding)
        {
            _entryFormatter = entryFormatter;
        }

        public void WriteEntries(IEnumerable<Entry> entries, EntryType entryType)
        {
            WriteLine(BuildHeader(entryType));

            foreach(var entry in entries)
            {
                WriteLine(_entryFormatter.AsCsv(entry));
            }
        }

        private string BuildHeader(EntryType entryType)
        {
            switch (entryType)
            {
                case EntryType.InstalledPrograms:
                case EntryType.Recents:
                    return string.Format(
                        "{0} ; {1} ; {2} ; {3}",
                        Activities_Inspector.Resources.MainWindows_InstallEntry_FileName,
                        Activities_Inspector.Resources.MainWindows_InstallEntry_DataSource,
                        Activities_Inspector.Resources.MainWindows_InstallEntry_FullPath,
                        Activities_Inspector.Resources.MainWindows_InstallEntry_installDate);

                case EntryType.Prefetch:
                    return string.Format(
                        "{0} ; {1} ; {2} ; {3}",
                        Activities_Inspector.Resources.MainWindows_Prefetch_ExecutableFileName,
                        Activities_Inspector.Resources.MainWindows_Prefetch_SourceFileName,
                        Activities_Inspector.Resources.MainWindows_Prefetch_Extension,
                        Activities_Inspector.Resources.MainWindows_Prefetch_LastRunTime);

                case EntryType.ShellBags:
                    return string.Format(
                        "{0} ; {1} ; {2}",
                        Activities_Inspector.Resources.MainWindows_ShellBags_AbsolutePath,
                        Activities_Inspector.Resources.MainWindows_ShellBags_LastRegistryWriteDate,
                        Activities_Inspector.Resources.MainWindows_ShellBags_RegistryPath);

                case EntryType.TimeIntervals:
                    return string.Format(
                        "{0} ; {1} ; {2} ; {3}",
                        Activities_Inspector.Resources.MainWindows_TimeIntervals_Start,
                        Activities_Inspector.Resources.MainWindows_TimeIntervals_End,
                        Activities_Inspector.Resources.MainWindows_TimeIntervals_Duration,
                        Activities_Inspector.Resources.MainWindows_TimeIntervals_MachineName);

                case EntryType.Sessions:
                    return string.Format(
                        "{0} ; {1} ; {2} ; {3} ; {4} ; {5} ; {6} ; {7}",
                        Activities_Inspector.Resources.MainWindows_Sessions_UserName,
                        Activities_Inspector.Resources.MainWindows_Sessions_Group,
                        Activities_Inspector.Resources.MainWindows_Sessions_MachineName,
                        Activities_Inspector.Resources.MainWindows_Sessions_LogOnTime,
                        Activities_Inspector.Resources.MainWindows_Sessions_LogOffTime,
                        Activities_Inspector.Resources.MainWindows_Sessions_Duration,
                        Activities_Inspector.Resources.MainWindows_Sessions_NetworkAddress,
                        Activities_Inspector.Resources.MainWindows_Sessions_AccessType);

                case EntryType.SystemTimeChanged:
                    return string.Format(
                        "{0} ; {1} ; {2} ; {3}",
                        Activities_Inspector.Resources.MainWindows_SystemTimeChanged_UserName,
                        Activities_Inspector.Resources.MainWindows_SystemTimeChanged_EventTime,
                        Activities_Inspector.Resources.MainWindows_SystemTimeChanged_OldTime,
                        Activities_Inspector.Resources.MainWindows_SystemTimeChanged_NewTime);

                case EntryType.Usb:
                    return string.Format(
                        "{0} ; {1} ; {2} ; {3} ; {4} ; {5} ; {6} ; {7}",
                        Activities_Inspector.Resources.MainWindow_Usb_State,
                        Activities_Inspector.Resources.MainWindow_Usb_DeviceName,
                        Activities_Inspector.Resources.MainWindow_Usb_SerialNumber,
                        Activities_Inspector.Resources.MainWindow_Usb_VendorId,
                        Activities_Inspector.Resources.MainWindow_Usb_ProductId,
                        Activities_Inspector.Resources.MainWindow_Usb_UsbClass,
                        Activities_Inspector.Resources.MainWindow_Usb_PluggedTime,
                        Activities_Inspector.Resources.MainWindow_Usb_UnpluggedTime);

                default: throw new ArgumentException(nameof(entryType));
            }
        }
    }
}
