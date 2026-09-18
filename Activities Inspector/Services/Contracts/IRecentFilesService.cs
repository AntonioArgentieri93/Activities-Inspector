using CSharpFunctionalExtensions;
using Activities_Inspector.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Activities_Inspector.Services
{
    public interface IRecentFilesService
    {
        Task<Result<List<RecentFolderEntry>>> GetRecentFilesAsync(CancellationToken cancellationToken = default);

        int SkippedFilesCount { get; }

        IReadOnlyList<IntegrityRecord> LastIntegrityManifest { get; }
    }
}