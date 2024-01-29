using ProgettoInformaticaForense_Argentieri.Models;

namespace ProgettoInformaticaForense_Argentieri.Services
{
    public interface IEntryFormatter
    {
        string AsCsv(Entry entry);
    }
}
