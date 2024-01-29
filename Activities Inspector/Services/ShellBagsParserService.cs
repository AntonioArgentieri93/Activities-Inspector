using CSharpFunctionalExtensions;
using ProgettoInformaticaForense_Argentieri.Models;
using ProgettoInformaticaForense_Argentieri.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace ProgettoInformaticaForense_Argentieri.Services
{
    public class ShellBagsParserService : IShellBagsParserService
    {
        private readonly FileLocations locations;

        public ShellBagsParserService()
        {
            locations = InitPaths();
        }

        public async Task<Result<List<IShellItem>>> ParseShellBags()
        {
            var taskCompletionSource = new TaskCompletionSource<Result<List<IShellItem>>>(
                TaskCreationOptions.RunContinuationsAsynchronously);

            try
            {
                var retList = new List<IShellItem>();

                await Task.Run(() =>
                {
                    var parser = new ConfigParser(locations.GUIDFileLocation, locations.OSFileLocation,
                        locations.ScriptFileLocation);

                    var onlineReader = new OnlineRegistryReader(parser, false);
                    retList.AddRange(ShellBagParser.GetShellItems(onlineReader));

                    taskCompletionSource.SetResult(Result.Success(retList));
                });
            }
            catch (Exception ex)
            {
                taskCompletionSource.SetResult(Result.Failure<List<IShellItem>>(ex.Message));
            }

            return taskCompletionSource.Task.Result;
        }

        private FileLocations InitPaths()
        {
            string workingRoot = Directory.GetCurrentDirectory();

            const string guids = Constants.GUID_FILE_PATH;
            const string os = Constants.OS_FILE_PATH;
            const string scripts = Constants.SCRIPTS_FILE_PATH;

            var guidsPath = Path.Combine(workingRoot, guids);
            var osPath = Path.Combine(workingRoot, os);
            var scriptsPath = Path.Combine(workingRoot, scripts);

            return new FileLocations(osPath, guidsPath, scriptsPath);
        }
    }
}
