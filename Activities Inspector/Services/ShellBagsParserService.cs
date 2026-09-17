using CSharpFunctionalExtensions;
using Activities_Inspector.Constants;
using Activities_Inspector.Models;
using Activities_Inspector.Utils;
using System;
using System.Collections.Generic;
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

        public async Task<Result<List<IShellItem>>> ParseShellBagsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var retList = new List<IShellItem>();

                await Task.Run(() =>
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var parser = new ConfigParser(_locations.GUIDFileLocation, _locations.OSFileLocation,
                        _locations.ScriptFileLocation);

                    var onlineReader = new OnlineRegistryReader(parser, false);
                    retList.AddRange(ShellBagParser.GetShellItems(onlineReader));
                }, cancellationToken);

                return Result.Success(retList);
            }
            catch (Exception ex) when (!(ex is OperationCanceledException))
            {
                return Result.Failure<List<IShellItem>>(ex.ToString());
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