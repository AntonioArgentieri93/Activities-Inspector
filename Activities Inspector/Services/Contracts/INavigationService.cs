using ProgettoInformaticaForense_Argentieri.Models;

namespace ProgettoInformaticaForense_Argentieri.Services
{
    public interface INavigationService
    {
        void Navigate(ViewerMode viewerMode);
        void ShowNavigator();
        void ShowSettingsWindow();
    }
}
