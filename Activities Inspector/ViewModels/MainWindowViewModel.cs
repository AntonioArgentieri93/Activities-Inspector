using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Activities_Inspector.Models;
using Activities_Inspector.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Activities_Inspector.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        #region Proprietà

        private string _evidenceSourceDescription;

        public string EvidenceSourceDescription
        {
            get => _evidenceSourceDescription;
            private set => Set(nameof(EvidenceSourceDescription), ref _evidenceSourceDescription, value);
        }

        #endregion

        #region Comandi

        private RelayCommand _selectLiveCommand;
        public RelayCommand SelectLiveCommand => _selectLiveCommand
            ?? (_selectLiveCommand = new RelayCommand(ExecuteSelectLiveCommand));

        private RelayCommand _selectImageCommand;
        public RelayCommand SelectImageCommand => _selectImageCommand
            ?? (_selectImageCommand = new RelayCommand(ExecuteSelectImageCommand));

        #endregion

        #region Navigazione

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
        private readonly Services.Evidence.IEvidenceSourceProvider _sources;
        private readonly IDialogService _dialogs;
        private readonly IAuditTrail _audit;

        public MainWindowViewModel(INavigationService navigationService,
            Services.Evidence.IEvidenceSourceProvider sources, IDialogService dialogs, IAuditTrail audit)
        {
            _navigationService = navigationService;
            _sources = sources;
            _dialogs = dialogs;
            _audit = audit;
            RefreshSourceDescription();
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

            // Selezione iniziale coerente con la pagina mostrata di default
            // nel Frame (TimeIntervals): senza questa, all'avvio nessuna
            // voce del menu laterale risulta selezionata.
            SelectedItem = Items.FirstOrDefault(item => item.ViewerMode == ViewerMode.TimeIntervals);
        }

        private void ExecuteSelectLiveCommand()
        {
            _sources.UseLive();
            RefreshSourceDescription();
            _audit.Record(AuditCategory.Sorgente, "Sistema live");
        }

        private void ExecuteSelectImageCommand()
        {
            var folder = _dialogs.SelectFolder("Seleziona la cartella radice dell'immagine acquisita");

            if (string.IsNullOrEmpty(folder)) return;

            try
            {
                _sources.UseImage(folder);
            }
            catch (Exception ex)
            {
                _dialogs.ShowError(ex.Message);
                return;
            }

            RefreshSourceDescription();
            _audit.Record(AuditCategory.Sorgente, $"Immagine {folder}");
        }

        private void RefreshSourceDescription()
            => EvidenceSourceDescription = $"Sorgente: {_sources.Current.DisplayName}";
    }
}

