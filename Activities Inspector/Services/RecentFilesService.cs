using CSharpFunctionalExtensions;
using Activities_Inspector.Constants;
using Activities_Inspector.Models;
using Activities_Inspector.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Activities_Inspector.Services
{
    public class RecentFilesService : IRecentFilesService
    {
        public int SkippedFilesCount { get; private set; }

        public async Task<Result<List<RecentFolderEntry>>> GetRecentFilesAsync(CancellationToken cancellationToken = default)
        {
            SkippedFilesCount = 0;

            try
            {
                var userName = Environment.UserName;
                var path = Path.Combine(@"C:\Users", userName, AppConstants.Paths.RecentDirectory);

                var directory = new DirectoryInfo(path);
                if (!directory.Exists)
                    return Result.Failure<List<RecentFolderEntry>>($"La cartella Recent non esiste: {path}");

                var files = directory.GetFiles(AppConstants.Paths.RecentExtension);
                var orderedFiles = files.OrderBy(f => f.LastWriteTime).ToList();

                var entries = new List<RecentFolderEntry>();

                foreach (var file in orderedFiles)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    LnkFile lnkFile = null;

                    try
                    {
                        lnkFile = await LoadFileAsync(file.FullName);
                    }
                    catch
                    {
                        SkippedFilesCount++;
                        continue;
                    }

                    if (lnkFile == null) continue;

                    var fullPath = ResolveTargetPath(
                        lnkFile.LocalPath,
                        lnkFile.NetworkShareInfo?.NetworkShareName,
                        lnkFile.CommonPath);
                    if (string.IsNullOrEmpty(fullPath)) continue;

                    var actionTime = file.LastWriteTime;
                    var fileName = Path.GetFileNameWithoutExtension(file.Name);
                    var dataSource = file.FullName;

                    var entry = new RecentFolderEntry(actionTime, fileName, dataSource, fullPath)
                    {
                        SkippedShellItems = lnkFile.SkippedShellItems
                    };
                    entries.Add(entry);
                }

                return Result.Success(entries);
            }
            catch (Exception ex) when (!(ex is OperationCanceledException))
            {
                return Result.Failure<List<RecentFolderEntry>>(ex.ToString());
            }
        }

        internal static string ResolveTargetPath(string localPath, string networkShareName, string commonPath)
        {
            if (!string.IsNullOrEmpty(localPath)) return localPath;

            if (string.IsNullOrEmpty(networkShareName))
                return commonPath ?? string.Empty;

            var share = networkShareName.TrimEnd('\\');
            var suffix = (commonPath ?? string.Empty).TrimStart('\\');

            return suffix.Length == 0 ? share : share + "\\" + suffix;
        }

        private Task<LnkFile> LoadFileAsync(string lnkFilePath, CancellationToken cancellationToken = default)
        {
            var raw = File.ReadAllBytes(lnkFilePath);
            if (raw.Length == 0 || raw[0] != 0x4c) return Task.FromResult<LnkFile>(null);
            return Task.FromResult(new LnkFile(raw, lnkFilePath));
        }
    }
}