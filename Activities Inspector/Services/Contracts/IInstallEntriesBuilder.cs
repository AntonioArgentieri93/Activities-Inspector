using CSharpFunctionalExtensions;
using ProgettoInformaticaForense_Argentieri.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ProgettoInformaticaForense_Argentieri.Services
{
    public interface IInstallEntriesBuilder
    {
        Task<Result<List<InstallEntry>>> GetInstallEntriesAsync(CancellationToken cancellationToken = default);
    }
}