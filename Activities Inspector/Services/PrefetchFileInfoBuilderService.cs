using CSharpFunctionalExtensions;
using ProgettoInformaticaForense_Argentieri.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ProgettoInformaticaForense_Argentieri.Services
{
    public class PrefetchFileInfoBuilderService : IPrefetchFileInfoBuilderService
    {
        private const string FILE_PATH = @"C:\Windows\prefetch";
        private const string EXTENSION = ".pf";

        IPrefetchFileParserService parser = new PrefetchFileParserService();

        public async Task<Result<List<PrefetchInfoEntry>>> GetPrefetchFileInfosAsync()
        {
            var taskCompletionSource = new TaskCompletionSource<Result<List<PrefetchInfoEntry>>>(
                TaskCreationOptions.RunContinuationsAsynchronously);

            var prefetchFileNames = GetPrefetchFilesNames().ToList();

            var tmp = new List<PrefetchInfoEntry>();

            try
            {
                await Task.Run(() =>
                {
                    foreach (var fileName in prefetchFileNames)
                    {
                        var pf = parser.Open(fileName);

                        if (pf == null) continue;

                        var executableFilename = pf.Header.ExecutableFilename; //Filename
                        var sourceFileName = pf.SourceFilename; //Data source
                        var lastRunTimes = pf.LastRunTimes;

                        var fileInfo = new FileInfo(executableFilename);

                        var extension = fileInfo.Extension; //File extension

                        foreach (var lastRunTime in lastRunTimes) //Action Time
                        {
                            tmp.Add(new PrefetchInfoEntry(executableFilename, sourceFileName, 
                                lastRunTime.LocalDateTime, extension));
                        }
                    }

                    taskCompletionSource.SetResult(Result.Success(tmp));
                });
            }
            catch (Exception ex)
            {
                taskCompletionSource.SetResult(Result.Failure<List<PrefetchInfoEntry>>(ex.Message));
            }

            return taskCompletionSource.Task.Result;
        }

        private IEnumerable<string> GetPrefetchFilesNames()
        {
            var prefetchFileNames = Directory.GetFiles(FILE_PATH).ToList();

            foreach (var fileName in prefetchFileNames)
            {
                var fileInfo = new FileInfo(fileName);

                if (fileInfo.Extension != EXTENSION) continue;

                yield return fileName;
            }
        }
    }
}
