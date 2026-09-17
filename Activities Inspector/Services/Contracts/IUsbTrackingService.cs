using CSharpFunctionalExtensions;
using Activities_Inspector.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Activities_Inspector.Services
{
    public interface IUsbTrackingService
    {
        Task<Result<List<UsbEntry>>> BuildUsbEntriesAsync(bool isAdministrator, CancellationToken cancellationToken = default);
    }
}