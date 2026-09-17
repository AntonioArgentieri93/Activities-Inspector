using CSharpFunctionalExtensions;
using Activities_Inspector.Constants;
using Activities_Inspector.Models;
using Activities_Inspector.Utils;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Activities_Inspector.Services
{
    public class ShellBagsParserService : IShellBagsParserService
    {
        private readonly FileLocations _locations;

        public ShellBagsParserService()
        {
            _locations = InitPaths();
        }

        public async Task<Result<ShellBagsResult>> ParseShellBagsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                ShellBagsResult result = null;

                await Task.Run(() =>
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var parser = new ConfigParser(_locations.GUIDFileLocation, _locations.OSFileLocation,
                        _locations.ScriptFileLocation);

                    var onlineReader = new OnlineRegistryReader(parser, false);
                    var (items, truncated) = ShellBagParser.GetShellItems(onlineReader);
                    result = new ShellBagsResult(items, truncated);
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