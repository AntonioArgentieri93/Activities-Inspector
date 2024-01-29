using CSharpFunctionalExtensions;
using ProgettoInformaticaForense_Argentieri.Models;
using System.Threading.Tasks;

namespace ProgettoInformaticaForense_Argentieri.Services
{
    public interface IReportService
    {
        Task<Result> CreatePdfFile(ProvisioningType provisioningType, string other, string inquirerSurname,
            string inquirerName, string inquirerQualification, string description,
            UsageInfo[] usageInfos, InstallEntry[] installedPrograms, RecentFolderEntry[] recentFolderEntries,
            PrefetchInfoEntry[] prefetchInfoEntries, ShellBagEntry[] shellBagEntries, SessionEntry[] sessionEntries,
            SystemTimeChangedEntry[] systemTimeChangedEntries, UsbEntry[] usbEntries, string destinationPath);
    }
}