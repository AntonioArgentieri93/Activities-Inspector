using CSharpFunctionalExtensions;
using Activities_Inspector.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Activities_Inspector.Services
{
    public interface ILoggedInfoService
    {
        Task<Result<List<SessionEntry>>> GetSessionsAsync(CancellationToken cancellationToken = default);
    }
}