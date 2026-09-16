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

        private List<LeftNavbarItem> _items;

        public List<LeftNavbarItem> Items
        {
            get => _items;
            set => Set(nameof(Items), ref _items, value);
        }

        private LeftNavbarItem _selectedItem;

        public LeftNavbarItem SelectedItem
        {
            get => _selectedItem;
            set
            {
                var changed = Set(nameof(SelectedItem), ref _selectedItem, value);

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
            var viewerModes = Enum.GetValues(typeof(ViewerMode)).Cast<ViewerMode>().ToList();
            
            Items = new List<LeftNavbarItem>();
            foreach (var mode in viewerModes)
            {
                var requiresAdminPrivileges = mode == ViewerMode.Prefetch ||
                    mode == ViewerMode.Sessions ||
                    mode == ViewerMode.ShellBags ||
                    mode == ViewerMode.SystemTimeChanged ||
                    mode == ViewerMode.Usb;

                Items.Add(new LeftNavbarItem(mode, requiresAdminPrivileges));
            }
        }
    }
}
