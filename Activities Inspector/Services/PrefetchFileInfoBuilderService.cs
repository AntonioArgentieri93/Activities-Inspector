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

                // Parsing CPU-bound su thread pool: senza questo, centinaia
                // di file verrebbero parsati sullo UI thread (freeze + spinner
                // mai renderizzato).
                var entries = await Task.Run(() =>
                {
                    var list = new List<PrefetchInfoEntry>();

                    foreach (var fileName in prefetchFileNames)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        var pf = _parser.Open(fileName);
                        if (pf == null) continue;

                        var fileInfo = new FileInfo(pf.Header.ExecutableFilename);
                        var entry = BuildEntry(pf, pf.Header.ExecutableFilename, pf.SourceFilename, fileInfo.Extension);
                        if (entry != null)
                            list.Add(entry);
                    }

                    return list;
                }, cancellationToken);

                return Result.Success(entries);
            }
            catch (Exception ex) when (!(ex is OperationCanceledException))
            {
                return Result.Failure<List<PrefetchInfoEntry>>(ex.ToString());
            }
        }

        internal static PrefetchInfoEntry BuildEntry(IPrefetch pf, string executableFilename, string sourceFileName, string extension)
        {
            if (pf.LastRunTimes.Count == 0) return null;

            return new PrefetchInfoEntry(executableFilename, sourceFileName,
                pf.LastRunTimes.Last().LocalDateTime, extension)
            {
                FirstRunTime = pf.LastRunTimes.First().LocalDateTime,
                RunCount = pf.RunCount
            };
        }

        private Task<List<string>> GetPrefetchFilesNamesAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Nessun try/catch qui: gli errori (es. accesso negato alla cartella
            // Prefetch) devono propagarsi al chiamante come Result.Failure,
            // non essere mascherati da lista vuota con successo.
            var files = Directory.GetFiles(AppConstants.Paths.PrefetchDirectory, AppConstants.Paths.PrefetchSearchPattern);
            return Task.FromResult(files.ToList());
        }
    }
}