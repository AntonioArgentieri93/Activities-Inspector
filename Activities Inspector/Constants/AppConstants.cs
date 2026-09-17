using System.Collections.Generic;

namespace Activities_Inspector.Constants
{
    public static class AppConstants
    {
        public static class Paths
        {
            public const string PrefetchDirectory = @"C:\Windows\Prefetch";
            public const string PrefetchExtension = ".pf";
            public const string PrefetchSearchPattern = "*.pf";
            public const string SystemHivePath = @"C:\Windows\System32\config\SYSTEM";
            public const string RegistrySystemPath = @"SYSTEM";
            public const string RecentDirectory = @"AppData\Roaming\Microsoft\Windows\Recent";
            public const string RecentExtension = "*.lnk";
        }

        public static class Registry
        {
            public const string UsbEnumPath = @"Enum\USB";
            public const string ControlSetPrefix = "ControlSet";
            public const string UsbDevicePropertiesGuid = "{83da6326-97a6-4088-9453-a1923f573b29}";
            public const string UsbLastConnectedValue = "0066";
            public const string UsbLastRemovedValue = "0067";
            public const string UsbRootPrefix = "ROOT";
            public const string Wow6432UninstallPath = @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall";
            public const string MicrosoftUninstallPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";
            public const string RegistrySystemPath = @"SYSTEM";
        }

        public static class EventLog
        {
            public const string SecurityLog = "Security";
            public const string ApplicationLog = "Application";
            public const int LogonEventId = 4624;
            public const int LogoffEventId = 4647;
            public const int SystemTimeChangedEventId = 4616;
        }

        public static class Prefetch
        {
            public const int Signature = 0x41434353; // 'SCCA'
            public const int HeaderSize = 4;
            public const int VersionOffset = 0;
            public const int SignatureOffset = 4;
        }

        public static class Lnk
        {
            public const int HeaderSize = 76;
            public const int ShellItemTerminator = 0;
            public const string UnknownShellItemContact = "saericzimmerman@gmail.com";
            public const string UnknownExtraBlockContact = "saericzimmerman@gmail.com";
        }

        public static class Ntp
        {
            public const string DefaultServer = "time.windows.com";
            public const int Port = 123;
            public const int TimeoutMs = 5000;
            public const int PacketSize = 48;
            public const byte RequestByte = 0x1B;
            public const long TicksPerSecond = 10000000L;
            public const long DaysTo1900 = 1900 * 365 + 95;
        }

        public static class Encoding
        {
            public const int DefaultCodePage = 1252;
        }

        public static class Usb
        {
            public static readonly IReadOnlyDictionary<string, string> ClassMap = new Dictionary<string, string>
            {
                ["01"] = "Audio",
                ["02"] = "Communications",
                ["03"] = "HID",
                ["05"] = "Physical",
                ["06"] = "Image",
                ["07"] = "Printer",
                ["08"] = "Mass Storage",
                ["09"] = "Hub",
                ["0a"] = "CDC Data",
                ["0b"] = "Smart Card",
                ["0d"] = "Content Security",
                ["0e"] = "Video",
                ["0f"] = "Personal Healthcare",
                ["dc"] = "Diagnostic Device",
                ["e0"] = "Wireless Controller",
                ["ef"] = "Miscellaneous",
                ["fe"] = "Application Specific",
                ["ff"] = "Vendor Specific"
            };
        }

        public static class Assets
        {
            public const string GuidsJson = @"Assets\GUIDs.json";
            public const string OsJson = @"Assets\OS.json";
            public const string ScriptsJson = @"Assets\Scripts.json";
        }

        public static class Assembly
        {
            public const string Namespace = "Activities_Inspector";
        }

        public static class ShellItems
        {
            public const string Size = "Size";
            public const string Type = "Type";
            public const string TypeName = "TypeName";
            public const string Name = "Name";
            public const string ModifiedDate = "ModifiedDate";
            public const string AccessedDate = "AccessedDate";
            public const string CreationDate = "CreationDate";
            public const string RegistryOwner = "RegistryOwner";
            public const string RegistrySid = "RegistrySID";
            public const string RegistryPath = "RegistryPath";
            public const string ShellbagPath = "ShellbagPath";
            public const string LastRegWrite = "LastRegistryWriteDate";
            public const string SlotModifiedDate = "SlotModifiedDate";
            public const string ExtensionVersion = "ExtensionVersion";
            public const string Signature = "Signature";
            public const string Properties = "properties";
            public const string ShellItem = "shellitem";
            public const string KnownGuids = "knownGUIDs";
            public const string LongName = "LongName";
            public const string LocalizedName = "LocalizedName";
            public const string Guid = "GUID";
            public const string FolderId = "FolderID";
            public const string Flags = "Flags";
            public const string FileSize = "FileSize";
            public const string FileAttributes = "FileAttributes";
            public const string ExtensionOffset = "ExtensionOffset";
            public const string ShortName = "ShortName";
            public const string Location = "Location";
            public const string Description = "Description";
            public const string Comments = "Comments";
            public const string Uri = "Uri";
            public const string FtpHostName = "FTPHostname";
            public const string FtpUserName = "FTPUsername";
            public const string FtpPassword = "FTPPassword";
            public const string ConnectionDate = "ConnectionDate";
            public const string DelegateItemId = "DelegateItemIdentifier";
            public const string ItemClassId = "ItemClassIdentifier";
        }
    }
}