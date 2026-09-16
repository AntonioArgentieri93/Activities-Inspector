using ProgettoInformaticaForense_Argentieri.Models;

namespace ProgettoInformaticaForense_Argentieri.Services
{
    public interface INavigationService
    {
        void Navigate(LeftNavbarItem item);
        void ShowNavigator();
        void ShowSettingsWindow();
    }
}
