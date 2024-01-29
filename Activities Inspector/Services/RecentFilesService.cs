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

        public async Task<Result<List<RecentFolderEntry>>> GetRecentFiles()
        {
            var taskCompletionSource = new TaskCompletionSource<Result<List<RecentFolderEntry>>>(
                TaskCreationOptions.RunContinuationsAsynchronously);

            try
            {
                string userName = Environment.UserName;

                var path = $@"C:\Users\{userName}\AppData\Roaming\Microsoft\Windows\Recent";

                var directory = new DirectoryInfo(path);

                if (directory.Exists == false) throw new ArgumentException("La cartella non esiste");

                var tmp = new List<RecentFolderEntry>();

                await Task.Run(() =>
                {
                    var files = new DirectoryInfo(path).GetFiles(FILE_EXTENSION);
                    var orderedFiles = files.OrderBy(fl => fl.LastWriteTime).ToList();

                    foreach (var file in orderedFiles)
                    {
                        var lnkFile = LoadFile(file.FullName);

                        if (lnkFile == null) continue;

                        if (string.IsNullOrEmpty(lnkFile.LocalPath)) continue;

                        var actionTime = file.LastWriteTime; //Action Time
                        var fileName = Path.GetFileNameWithoutExtension(file.Name); //Filename
                        var dataSource = file.FullName; //Data Source
                        var fullPath = lnkFile.LocalPath + lnkFile.CommonPath; //Full Path

                        tmp.Add(new RecentFolderEntry(actionTime, fileName, dataSource, fullPath));
                    }

                    taskCompletionSource.SetResult(Result.Success(tmp));
                });
            }
            catch (Exception ex)
            {
                taskCompletionSource.SetResult(Result.Failure<List<RecentFolderEntry>>(ex.Message));
            }

            return taskCompletionSource.Task.Result;
        }

        private LnkFile LoadFile(string lnkFile)
        {
            var raw = File.ReadAllBytes(lnkFile);

            return raw[0] != 0x4c ? null : new LnkFile(raw, lnkFile);
        }
    }
}
