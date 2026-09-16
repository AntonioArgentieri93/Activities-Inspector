using CSharpFunctionalExtensions;
using ProgettoInformaticaForense_Argentieri.Constants;
using ProgettoInformaticaForense_Argentieri.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ProgettoInformaticaForense_Argentieri.Services
{
    public class PrefetchFileInfoBuilderService : IPrefetchFileInfoBuilderService
    {
        private readonly IPrefetchFileParserService _parser;

        public PrefetchFileInfoBuilderService(IPrefetchFileParserService parser)
        {
            _parser = parser;
        }

        public async Task<Result<List<PrefetchInfoEntry>>> GetPrefetchFileInfosAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var prefetchFileNames = await GetPrefetchFilesNamesAsync(cancellationToken);

                var entries = new List<PrefetchInfoEntry>();

                foreach (var fileName in prefetchFileNames)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var pf = _parser.Open(fileName);
                    if (pf == null) continue;

                    var executableFilename = pf.Header.ExecutableFilename;
                    var sourceFileName = pf.SourceFilename;
                    var lastRunTimes = pf.LastRunTimes;

                    if (lastRunTimes.Count == 0) continue;

                    var fileInfo = new FileInfo(executableFilename);
                    var extension = fileInfo.Extension;
                    var lastRunTime = lastRunTimes.Last();

                    entries.Add(new PrefetchInfoEntry(executableFilename, sourceFileName, lastRunTime.LocalDateTime, extension));
                }

                return Result.Success(entries);
            }
            catch (Exception ex) when (!(ex is OperationCanceledException))
            {
                return Result.Failure<List<PrefetchInfoEntry>>(ex.ToString());
            }
        }

        private Task<List<string>> GetPrefetchFilesNamesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var files = Directory.GetFiles(AppConstants.Paths.PrefetchDirectory, AppConstants.Paths.PrefetchExtension);
                return Task.FromResult(files.ToList());
            }
            catch (Exception ex) when (!(ex is OperationCanceledException))
            {
                return Task.FromResult(new List<string>());
            }
        }
    }
}