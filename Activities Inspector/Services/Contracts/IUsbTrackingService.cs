using CSharpFunctionalExtensions;
using ProgettoInformaticaForense_Argentieri.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProgettoInformaticaForense_Argentieri.Services
{
    public interface IUsbTrackingService
    {
        Task<Result<List<UsbEntry>>> BuildUsbEntries(bool isAdministrator);
    }
}
