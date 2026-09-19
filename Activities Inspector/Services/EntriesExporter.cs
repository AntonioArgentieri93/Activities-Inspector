using CSharpFunctionalExtensions;
using Activities_Inspector.Models;
using Activities_Inspector.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Activities_Inspector.Services
{
    public class EntriesExporter : IEntriesExporter
    {
        private readonly IEntryFormatter _entryFormatter;

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
                var filePath = Path.Combine(root, $"{fileName}_{DateTime.Now:dd-M-yyyy_HH-mm-ss}.csv");

                using (var writer = new EntryWriter(filePath, false, Encoding.Default, _entryFormatter))
                {
                    writer.WriteEntries(entries, entryType, footerNote);
                }

                var bytes = await File.ReadAllBytesAsync(filePath, cancellationToken);
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