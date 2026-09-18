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
    public class ShellBagsParserService : IShellBagsParserService
    {
        private readonly FileLocations _locations;
        private readonly Evidence.IEvidenceSourceProvider _sources;

        public ShellBagsParserService(Evidence.IEvidenceSourceProvider sources)
        {
            _sources = sources;
            _locations = InitPaths();
        }

        public IReadOnlyList<IntegrityRecord> LastIntegrityManifest { get; private set; }
            = new List<IntegrityRecord>();

        public async Task<Result<ShellBagsResult>> ParseShellBagsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                ShellBagsResult result = null;

                await Task.Run(() =>
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var manifest = new List<IntegrityRecord>();
                    LastIntegrityManifest = manifest;

                    var parser = new ConfigParser(_locations.GUIDFileLocation, _locations.OSFileLocation,
                        _locations.ScriptFileLocation);

                    if (_sources.Current.IsLive)
                    {
                        manifest.Add(IntegrityRecord.LiveSource(EntryType.ShellBags,
                            "Registro di sistema (API live)"));

                        var onlineReader = new OnlineRegistryReader(parser, false);
                        var (items, truncated) = ShellBagParser.GetShellItems(onlineReader);
                        result = new ShellBagsResult(items, truncated, manifest);
                    }
                    else
                    {
                        var hives = _sources.Current.GetUserHivePaths("NTUSER.DAT")
                            .Concat(_sources.Current.GetUserHivePaths(@"AppData\Local\Microsoft\Windows\UsrClass.dat"))
                            .ToList();

                        if (hives.Count == 0)
                            throw new InvalidOperationException("Nessun hive utente (NTUSER.DAT/UsrClass.dat) nell'immagine.");

                        var items = new List<IShellItem>();
                        var truncated = false;

                        foreach (var hive in hives)
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            manifest.Add(IntegrityHasher.HashFile(hive, EntryType.ShellBags));

                            var reader = new OfflineRegistryReader(parser, hive);
                            var (part, partTruncated) = ShellBagParser.GetShellItems(reader);
                            items.AddRange(part);
                            truncated = truncated || partTruncated;
                        }

                        result = new ShellBagsResult(items, truncated, manifest);
                    }
                }, cancellationToken);

                return Result.Success(result);
            }
            catch (Exception ex) when (!(ex is OperationCanceledException))
            {
                return Result.Failure<ShellBagsResult>(ex.ToString());
            }
        }

        private FileLocations InitPaths()
        {
            string workingRoot = Directory.GetCurrentDirectory();

            var guidsPath = Path.Combine(workingRoot, AppConstants.Assets.GuidsJson);
            var osPath = Path.Combine(workingRoot, AppConstants.Assets.OsJson);
            var scriptsPath = Path.Combine(workingRoot, AppConstants.Assets.ScriptsJson);

            return new FileLocations(osPath, guidsPath, scriptsPath);
        }
    }
}