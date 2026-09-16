using CSharpFunctionalExtensions;
using ProgettoInformaticaForense_Argentieri.Models;
using ProgettoInformaticaForense_Argentieri.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ProgettoInformaticaForense_Argentieri.Services
{
    public class RecentFilesService : IRecentFilesService
    {
        private const string FILE_EXTENSION = @"*.lnk";

        public async Task<Result<List<RecentFolderEntry>>> GetRecentFilesAsync()
        {
            return await Task.Run(async () =>
            {
                string userName = Environment.UserName;

                var path = $@"C:\Users\{userName}\AppData\Roaming\Microsoft\Windows\Recent";

                var directory = new DirectoryInfo(path);

                if (directory.Exists == false) throw new ArgumentException("La cartella non esiste");

                var tmp = new List<RecentFolderEntry>();

                try
                {
                    var files = new DirectoryInfo(path).GetFiles(FILE_EXTENSION);
                    var orderedFiles = files.OrderBy(fl => fl.LastWriteTime).ToList();

                    foreach (var file in orderedFiles)
                    {
                        var lnkFile = await LoadFileAsync(file.FullName);

                        if (lnkFile == null) continue;

                        if (string.IsNullOrEmpty(lnkFile.LocalPath)) continue;

                        var actionTime = file.LastWriteTime; //Action Time
                        var fileName = Path.GetFileNameWithoutExtension(file.Name); //Filename
                        var dataSource = file.FullName; //Data Source
                        var fullPath = lnkFile.LocalPath; //Full Path

                        tmp.Add(new RecentFolderEntry(actionTime, fileName, dataSource, fullPath));
                    }

                    return Result.Success(tmp);
                }
                catch (Exception ex)
                {
                    return Result.Failure<List<RecentFolderEntry>>(ex.Message);
                }
            });
        }

        private async Task<LnkFile> LoadFileAsync(string lnkFile)
        {
            var raw = await File.ReadAllBytesAsync(lnkFile);

            return raw[0] != 0x4c ? null : new LnkFile(raw, lnkFile);
        }
    }
}
