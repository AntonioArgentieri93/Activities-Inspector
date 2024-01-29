using GalaSoft.MvvmLight;
using ProgettoInformaticaForense_Argentieri.Models;
using ProgettoInformaticaForense_Argentieri.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ProgettoInformaticaForense_Argentieri.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        #region Proprietà

        private List<ViewerMode> _viewerModes;

        public List<ViewerMode> ViewerModes
        {
            get => _viewerModes;
            set => Set(nameof(ViewerModes), ref _viewerModes, value);
        }

        private ViewerMode _selectedViewerMode;

        public ViewerMode SelectedViewerMode
        {
            get => _selectedViewerMode;
            set
            {
                var changed = Set(nameof(SelectedViewerMode), ref _selectedViewerMode, value);

                if (changed)
                {
                    _navigationService.Navigate(value);
                }
            }
        }

        #endregion

        private readonly INavigationService _navigationService;

        public MainWindowViewModel(INavigationService navigationService)
        {
            _navigationService = navigationService;
            ViewerModes = Enum.GetValues(typeof(ViewerMode)).Cast<ViewerMode>().Cast<ViewerMode>().ToList();
        }
    }
}
