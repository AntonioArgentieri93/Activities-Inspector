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
            return await Task.Run(async () =>
            {
                try
                {
                    var prefetchFileNames = (await GetPrefetchFilesNamesAsync()).ToList();

                    var tmp = new List<PrefetchInfoEntry>();

                    foreach (var fileName in prefetchFileNames)
                    {
                        var pf = parser.Open(fileName);

                        if (pf == null) continue;

                        var executableFilename = pf.Header.ExecutableFilename; //Filename
                        var sourceFileName = pf.SourceFilename; //Data source
                        var lastRunTimes = pf.LastRunTimes;

                        var fileInfo = new FileInfo(executableFilename);

                        var extension = fileInfo.Extension; //File extension

                        var lastRunTime = lastRunTimes.Last(); //Action Time
                        tmp.Add(new PrefetchInfoEntry(executableFilename, sourceFileName,
                            lastRunTime.LocalDateTime, extension));
                    }

                    return Result.Success(tmp);
                }
                catch (Exception ex)
                {
                    return Result.Failure<List<PrefetchInfoEntry>>(ex.Message);
                }
            });
        }

        private async Task<IEnumerable<string>> GetPrefetchFilesNamesAsync()
        {
            return await Task.Run(() =>
            {
                var prefetchFileNames = Directory.GetFiles(FILE_PATH, "*.pf").ToList();

                return prefetchFileNames;
            });
        }
    }
}
