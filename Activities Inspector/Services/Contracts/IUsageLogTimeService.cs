using CSharpFunctionalExtensions;
using ProgettoInformaticaForense_Argentieri.Models;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace ProgettoInformaticaForense_Argentieri.Services
{
    public interface IUsageLogTimeService
    {
        Task<Result<List<EventLogEntry>>> GetSystemEventsAsync();
        IEnumerable<UsageInfo> BuildUsageInfo(IEnumerable<EventLogEntry> events);
    }
}
