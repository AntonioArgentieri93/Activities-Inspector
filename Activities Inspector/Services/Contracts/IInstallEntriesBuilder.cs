using CSharpFunctionalExtensions;
using Activities_Inspector.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Activities_Inspector.Services
{
    public interface IInstallEntriesBuilder
    {
        Task<Result<List<InstallEntry>>> GetInstallEntriesAsync(CancellationToken cancellationToken = default);
    }
}