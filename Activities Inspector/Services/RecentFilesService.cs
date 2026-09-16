using CSharpFunctionalExtensions;
using ProgettoInformaticaForense_Argentieri.Constants;
using ProgettoInformaticaForense_Argentieri.Models;
using ProgettoInformaticaForense_Argentieri.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ProgettoInformaticaForense_Argentieri.Services
{
    public class RecentFilesService : IRecentFilesService
    {
        public async Task<Result<List<RecentFolderEntry>>> GetRecentFilesAsync(CancellationToken cancellationToken = default)
        {
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

                    var lnkFile = await LoadFileAsync(file.FullName);
                    if (lnkFile == null || string.IsNullOrEmpty(lnkFile.LocalPath)) continue;

                    var actionTime = file.LastWriteTime;
                    var fileName = Path.GetFileNameWithoutExtension(file.Name);
                    var dataSource = file.FullName;
                    var fullPath = lnkFile.LocalPath;

                    entries.Add(new RecentFolderEntry(actionTime, fileName, dataSource, fullPath));
                }

                return Result.Success(entries);
            }
            catch (Exception ex) when (!(ex is OperationCanceledException))
            {
                return Result.Failure<List<RecentFolderEntry>>(ex.ToString());
            }
        }

        private Task<LnkFile?> LoadFileAsync(string lnkFilePath, CancellationToken cancellationToken = default)
        {
            try
            {
                var raw = File.ReadAllBytes(lnkFilePath);
                return Task.FromResult<LnkFile?>(raw.Length > 0 && raw[0] == 0x4c ? new LnkFile(raw, lnkFilePath) : null);
            }
            catch
            {
                return Task.FromResult<LnkFile?>(null);
            }
        }
    }
}