using Activities_Inspector.Models;

namespace Activities_Inspector.Services
{
    public interface INavigationService
    {
        void Navigate(LeftNavbarItem item);
        void ShowNavigator();
        void ShowSettingsWindow();
    }
}
