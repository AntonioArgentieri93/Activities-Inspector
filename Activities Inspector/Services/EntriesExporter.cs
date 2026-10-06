using CSharpFunctionalExtensions;
using Activities_Inspector.Models;
using Activities_Inspector.Utils;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Activities_Inspector.Services
{
    public class EntriesExporter : IEntriesExporter
    {
        private readonly IEntryFormatter _entryFormatter;

        private const int MaxNameAttempts = 10000;

        public EntriesExporter(IEntryFormatter entryFormatter)
        {
            _entryFormatter = entryFormatter;
            InitFileSystem();
        }

        public async Task<Result<string>> SaveEntriesDataAsync(IEnumerable<Entry> entries, EntryType entryType, CancellationToken cancellationToken = default, string footerNote = null)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var fileName = SetFileName(entryType);
                var root = ExportLocations.OutputDirectory();
                var basePath = Path.Combine(root, $"{fileName}_{DateTime.Now.ToString("dd-MM-yyyy_HH-mm-ss", CultureInfo.InvariantCulture)}");

                // Il nome si riserva creando il file con FileMode.CreateNew (atomico): con "controlla se esiste, poi
                // crea" due esportazioni dello stesso tipo nello stesso secondo sceglievano lo stesso nome e una
                // falliva (file in uso) o sovrascriveva l'altra
                var filePath = string.Empty;
                FileStream stream = null;
                for (var counter = 0; stream == null; counter++)
                {
                    if (counter > MaxNameAttempts)
                        throw new IOException($"Impossibile creare un nome file univoco per {basePath}.csv");

                    filePath = counter == 0 ? basePath + ".csv" : $"{basePath}_{counter}.csv";

                    try
                    {
                        stream = new FileStream(filePath, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
                    }
                    catch (IOException) when (File.Exists(filePath))
                    {
                        // nome gia' preso da un'altra esportazione: prova il successivo
                    }
                }

                using (var writer = new EntryWriter(stream, Encoding.Default, _entryFormatter))
                {
                    writer.WriteEntries(entries, entryType, footerNote);
                }

                var bytes = await FileRetry.ReadAllBytesAsync(filePath, cancellationToken);
                IntegrityHasher.WriteSidecar(filePath, bytes);

                return Result.Success(filePath);
            }
            catch (Exception ex) when (!(ex is OperationCanceledException))
            {
                return Result.Failure<string>(ex.ToString());
            }
        }

        private void InitFileSystem()
        {
            if (!Directory.Exists(ExportLocations.OutputDirectory()))
                Directory.CreateDirectory(ExportLocations.OutputDirectory());
        }

        private static string SetFileName(EntryType entryType)
        {
            return entryType switch
            {
                EntryType.TimeIntervals => Activities_Inspector.Resources.Intervals_FileName,
                EntryType.InstalledPrograms => Activities_Inspector.Resources.InstalledPrograms_FileName,
                EntryType.Recents => Activities_Inspector.Resources.Recents_FileName,
                EntryType.Prefetch => Activities_Inspector.Resources.Prefetch_FileName,
                EntryType.ShellBags => Activities_Inspector.Resources.ShellBags_FileName,
                EntryType.Sessions => Activities_Inspector.Resources.Sessions_FileName,
                EntryType.SystemTimeChanged => Activities_Inspector.Resources.SystemTimeChanged_FileName,
                EntryType.Usb => Activities_Inspector.Resources.Usb_FileName,
                _ => throw new ArgumentException($"Unknown entry type: {entryType}")
            };
        }
    }
}