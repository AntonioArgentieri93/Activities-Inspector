using System.Collections.Generic;

namespace Activities_Inspector.Services.Evidence
{
    public interface IEvidenceSource
    {
        bool IsLive { get; }
        string DisplayName { get; }

        string MapPath(string windowsAbsolutePath);

        IEnumerable<string> EnumerateFiles(string windowsDirectory, string searchPattern);

        IEnumerable<string> GetUserProfileDirs();

        IEnumerable<string> GetRecentDirectories();

        IEnumerable<string> GetStartMenuDirectories();

        string GetSystemHivePath();

        string GetSoftwareHivePath();

        IEnumerable<string> GetUserHivePaths(string fileName);

        string GetEventLogPath(string logName);
    }
}
