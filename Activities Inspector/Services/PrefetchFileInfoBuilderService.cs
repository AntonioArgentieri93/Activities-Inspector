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
        private readonly Evidence.IEvidenceSourceProvider _sources;

        public IReadOnlyList<IntegrityRecord> LastIntegrityManifest { get; private set; }
            = new List<IntegrityRecord>();

        public PrefetchFileInfoBuilderService(IPrefetchFileParserService parser, Evidence.IEvidenceSourceProvider sources)
        {
            _parser = parser;
            _sources = sources;
        }

        public async Task<Result<List<PrefetchInfoEntry>>> GetPrefetchFileInfosAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                LastIntegrityManifest = new List<IntegrityRecord>();

                var prefetchFileNames = await GetPrefetchFilesNamesAsync(cancellationToken);

                // Parsing CPU-bound su thread pool: senza questo, centinaia
                // di file verrebbero parsati sullo UI thread (freeze + spinner
                // mai renderizzato).
                var entries = await Task.Run(() =>
                {
                    var list = new List<PrefetchInfoEntry>();
                    var manifest = new List<IntegrityRecord>();

                    foreach (var fileName in prefetchFileNames)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        byte[] raw;
                        try
                        {
                            raw = File.ReadAllBytes(fileName);
                        }
                        catch
                        {
                            manifest.Add(IntegrityHasher.HashFile(fileName, EntryType.Prefetch));
                            continue;
                        }

                        manifest.Add(IntegrityHasher.HashBytes(raw, fileName, EntryType.Prefetch));

                        IPrefetch pf;
                        try
                        {
                            pf = _parser.Open(new MemoryStream(raw), fileName);
                        }
                        catch
                        {
                            continue;
                        }

                        if (pf == null) continue;

                        var exeName = ResolveExecutableName(pf.Header.ExecutableFilename, pf.Filenames);
                        var entry = BuildEntry(pf, exeName, pf.SourceFilename, Path.GetExtension(exeName));
                        if (entry != null)
                            list.Add(entry);
                    }

                    LastIntegrityManifest = manifest;
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

            // Il Prefetch conserva fino a 8 timestamp (dal più recente al più vecchio): Max/Min non dipendono dall'ordine.
            // La più vecchia è la prima esecuzione reale solo se tutte le esecuzioni sono registrate
            // (RunCount <= timestamp presenti); altrimenti la prima esecuzione non è documentata → N/D.
            var newest = pf.LastRunTimes.Max();
            var oldest = pf.LastRunTimes.Min();
            bool allRunsRecorded = pf.RunCount > 0 && pf.RunCount <= pf.LastRunTimes.Count;

            return new PrefetchInfoEntry(executableFilename, sourceFileName, newest.LocalDateTime, extension)
            {
                FirstRunTime = allRunsRecorded ? oldest.LocalDateTime : (DateTime?)null,
                RunCount = pf.RunCount
            };
        }

        /// <summary>Lunghezza massima del nome eseguibile nell'header Prefetch (60 byte UTF-16 = 29 caratteri + terminatore).</summary>
        internal const int HeaderExecutableNameMaxLength = 29;

        /// <summary>
        /// Nome completo dell'eseguibile. L'header lo tronca a 29 caratteri ("MICROSOFT.AAD.BROKERPLUGIN.EX"):
        /// in quel caso lo si cerca nell'elenco dei file caricati (Filenames) tra quelli che iniziano col nome troncato,
        /// preferendo .EXE. Nomi non troncati restano quelli dell'header (affidabili).
        /// </summary>
        internal static string ResolveExecutableName(string headerName, IEnumerable<string> filenames)
        {
            if (string.IsNullOrEmpty(headerName) || headerName.Length < HeaderExecutableNameMaxLength || filenames == null)
                return headerName;

            var candidates = filenames
                .Where(f => !string.IsNullOrEmpty(f))
                .Select(f => Path.GetFileName(f.TrimEnd('\0')))
                .Where(n => n.Length > headerName.Length && n.StartsWith(headerName, StringComparison.OrdinalIgnoreCase))
                .ToList();

            return candidates.FirstOrDefault(n => n.EndsWith(".EXE", StringComparison.OrdinalIgnoreCase))
                ?? candidates.FirstOrDefault()
                ?? headerName;
        }

        private Task<List<string>> GetPrefetchFilesNamesAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

                // Nessun try/catch qui: gli errori (es. accesso negato alla cartella
                // Prefetch) devono propagarsi al chiamante come Result.Failure,
                // non essere mascherati da lista vuota con successo.
                var files = _sources.Current.EnumerateFiles(
                    AppConstants.Paths.PrefetchDirectory, AppConstants.Paths.PrefetchSearchPattern);
            return Task.FromResult(files.ToList());
        }
    }
}