using CSharpFunctionalExtensions;
using Activities_Inspector.Models;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Activities_Inspector.Services
{
    public interface IUsageLogTimeService
    {
        Task<Result<List<EventLogEntry>>> GetSystemEventsAsync(CancellationToken cancellationToken = default);
        IEnumerable<UsageInfo> BuildUsageInfo(IEnumerable<EventLogEntry> events);
    }
}