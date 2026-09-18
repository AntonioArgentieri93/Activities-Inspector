using System;
using System.IO;
using System.Linq;

namespace Activities_Inspector.Services
{
    internal static class ExportLocations
    {
        internal static string RemovableRoot()
            => DriveInfo.GetDrives()
                .Where(d => d.DriveType == DriveType.Removable && d.IsReady)
                .OrderBy(d => d.Name)
                .Select(d => d.RootDirectory.FullName)
                .FirstOrDefault();

        internal static string OutputDirectory()
            => Path.Combine(RemovableRoot() ?? AppDomain.CurrentDomain.BaseDirectory, "Output");

        internal static bool IsRemovable(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;

            var root = Path.GetPathRoot(Path.GetFullPath(path));
            return DriveInfo.GetDrives()
                .Where(d => d.DriveType == DriveType.Removable && d.IsReady)
                .Any(d => string.Equals(
                    d.RootDirectory.FullName.TrimEnd(Path.DirectorySeparatorChar),
                    root.TrimEnd(Path.DirectorySeparatorChar),
                    StringComparison.OrdinalIgnoreCase));
        }
    }
}
