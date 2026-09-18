using Activities_Inspector.Constants;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Activities_Inspector.Services.Evidence
{
    public sealed class LiveEvidenceSource : IEvidenceSource
    {
        public bool IsLive => true;
        public string DisplayName => "Sistema live";

        public string MapPath(string windowsAbsolutePath) => windowsAbsolutePath;

        public IEnumerable<string> EnumerateFiles(string windowsDirectory, string searchPattern)
        {
            return Directory.GetFiles(windowsDirectory, searchPattern);
        }

        public IEnumerable<string> GetUserProfileDirs()
        {
            var current = Path.Combine(@"C:\Users", Environment.UserName);
            if (Directory.Exists(current)) return new[] { current };
            return Enumerable.Empty<string>();
        }

        public IEnumerable<string> GetRecentDirectories()
        {
            return GetUserProfileDirs()
                .Select(p => Path.Combine(p, AppConstants.Paths.RecentDirectory));
        }

        public IEnumerable<string> GetStartMenuDirectories()
        {
            var candidates = new[]
            {
                Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu),
                    "Programs"),
                Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),
                    "Programs")
            };

            return candidates.Where(Directory.Exists);
        }

        public string GetSystemHivePath() => AppConstants.Paths.SystemHivePath;

        public string GetSoftwareHivePath() => @"C:\Windows\System32\config\SOFTWARE";

        public string GetEventLogPath(string logName)
            => Path.Combine(Environment.SystemDirectory, "winevt", "Logs", logName + ".evtx");

        public IEnumerable<string> GetUserHivePaths(string fileName)
        {
            return GetUserProfileDirs()
                .Select(p => Path.Combine(p, fileName))
                .Where(File.Exists);
        }
    }
}
