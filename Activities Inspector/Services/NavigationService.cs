using ProgettoInformaticaForense_Argentieri.Models;
using ProgettoInformaticaForense_Argentieri.Views;
using System;
using System.Collections.Generic;
using System.Windows;

namespace ProgettoInformaticaForense_Argentieri.Services
{
    public class NavigationService : INavigationService
    {
        private ViewerMode _lastViewerMode = ViewerMode.TimeIntervals;

        public void Navigate(ViewerMode viewerMode)
        {
            if (_lastViewerMode == viewerMode) return;

            var window = (Application.Current.MainWindow);
            var frame = ((MainWindow)window).Frame;

            var rawPageUri = _uri[viewerMode];

            var pageUri = new Uri(rawPageUri, UriKind.Relative);

            frame.Navigate(pageUri);

            _lastViewerMode = viewerMode;
        }

        public void ShowNavigator()
        {
            throw new NotImplementedException();
        }

        public void ShowSettingsWindow()
        {
            throw new NotImplementedException();
        }

        private readonly IReadOnlyDictionary<ViewerMode, string> _uri = new Dictionary<ViewerMode, string>
        {
            { ViewerMode.TimeIntervals,                   "Pages/TimeIntervals.xaml" },
            { ViewerMode.InstalledPrograms,             "Pages/InstalledPrograms.xaml" },
            { ViewerMode.Recents,             "Pages/Recents.xaml" },
            { ViewerMode.Prefetch,             "Pages/Prefetch.xaml" },
            { ViewerMode.ShellBags,     "Pages/ShellBags.xaml" },
            { ViewerMode.Sessions,     "Pages/Sessions.xaml" },
            { ViewerMode.SystemTimeChanged,     "Pages/SystemTimeChanged.xaml" },
            { ViewerMode.Usb,     "Pages/Usb.xaml" }
        };
    }
}
