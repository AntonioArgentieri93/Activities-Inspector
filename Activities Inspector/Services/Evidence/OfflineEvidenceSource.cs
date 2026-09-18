using Activities_Inspector.Constants;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Activities_Inspector.Services.Evidence
{
    public sealed class OfflineEvidenceSource : IEvidenceSource
    {
        private static readonly Regex DrivePrefix = new Regex(@"^[A-Za-z]:", RegexOptions.Compiled);

        public string RootPath { get; }

        public OfflineEvidenceSource(string rootPath)
        {
            var full = Path.GetFullPath(rootPath);
            RootPath = full.Length > 3
                ? full.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                : full;
        }

        public bool IsLive => false;
        public string DisplayName => $"Immagine: {RootPath}";

        public string MapPath(string windowsAbsolutePath)
        {
            var relative = DrivePrefix.Replace(windowsAbsolutePath, string.Empty).TrimStart(Path.DirectorySeparatorChar);
            return Path.Combine(RootPath, relative);
        }

        public IEnumerable<string> EnumerateFiles(string windowsDirectory, string searchPattern)
        {
            return Directory.GetFiles(MapPath(windowsDirectory), searchPattern);
        }

        public IEnumerable<string> GetUserProfileDirs()
        {
            var users = Path.Combine(RootPath, "Users");
            if (!Directory.Exists(users)) return Enumerable.Empty<string>();
            return Directory.GetDirectories(users);
        }

        public IEnumerable<string> GetRecentDirectories()
        {
            return GetUserProfileDirs()
                .Select(p => Path.Combine(p, AppConstants.Paths.RecentDirectory))
                .Where(Directory.Exists);
        }

        public IEnumerable<string> GetStartMenuDirectories()
        {
            var candidates = new List<string>
            {
                MapPath(Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu),
                    "Programs"))
            };

            foreach (var profile in GetUserProfileDirs())
            {
                candidates.Add(Path.Combine(profile,
                    @"AppData\Roaming\Microsoft\Windows\Start Menu\Programs"));
            }

            return candidates.Where(Directory.Exists).Distinct(StringComparer.OrdinalIgnoreCase);
        }

        public string GetSystemHivePath() => MapPath(AppConstants.Paths.SystemHivePath);

        public string GetSoftwareHivePath() => MapPath(@"C:\Windows\System32\config\SOFTWARE");

        public string GetEventLogPath(string logName)
            => MapPath(Path.Combine(Environment.SystemDirectory, "winevt", "Logs", logName + ".evtx"));

        public IEnumerable<string> GetUserHivePaths(string fileName)
        {
            return GetUserProfileDirs()
                .Select(p => Path.Combine(p, fileName))
                .Where(File.Exists);
        }
    }
}
