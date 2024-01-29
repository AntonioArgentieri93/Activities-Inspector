using Activities_Inspector.Messages;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using ProgettoInformaticaForense_Argentieri.Messages;
using ProgettoInformaticaForense_Argentieri.Models;
using ProgettoInformaticaForense_Argentieri.Pages;
using ProgettoInformaticaForense_Argentieri.Services;
using RawCopy;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace ProgettoInformaticaForense_Argentieri.ViewModels
{
    public class PrefetchViewModel : ViewModelBase
    {
        #region Proprietà

        private ObservableCollection<PrefetchInfoEntry> _prefetchEntries;

        public ObservableCollection<PrefetchInfoEntry> PrefetchEntries
        {
            get => _prefetchEntries;
            set
            {
                var changed = Set(nameof(PrefetchEntries), ref _prefetchEntries, value);

                if (changed)
                {
                    ExportCommand.RaiseCanExecuteChanged();
                }
            }
        }

        private bool _isBusy;

        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                var changed = Set(nameof(IsBusy), ref _isBusy, value);

                if (changed)
                {
                    LoadPrefetchInfoEntriesCommand.RaiseCanExecuteChanged();
                    ExportCommand.RaiseCanExecuteChanged();
                }
            }
        }

        #endregion

        #region Comandi

        private RelayCommand _loadPrefetchInfoEntriesCommand;
        public RelayCommand LoadPrefetchInfoEntriesCommand => _loadPrefetchInfoEntriesCommand
            ?? (_loadPrefetchInfoEntriesCommand = new RelayCommand(ExecuteLoadPrefetchInfoEntriesCommandAsync,
                CanExecuteLoadPrefetchInfoEntriesCommand));

        private RelayCommand _exportCommand;
        public RelayCommand ExportCommand => _exportCommand
            ?? (_exportCommand = new RelayCommand(ExecuteExportCommandAsync,
                CanExecuteExportCommandAsync));

        #endregion

        private readonly IPrefetchFileInfoBuilderService _prefetchFileInfoBuilderService;
        private readonly IDialogService _dialogService;
        private readonly IEntriesExporter _entriesExporter;
        private readonly IMessenger _messenger;

        public PrefetchViewModel(IPrefetchFileInfoBuilderService prefetchFileInfoBuilderService,
            IDialogService dialogService, IEntriesExporter entriesExporter, IMessenger messenger)
        {
            _prefetchFileInfoBuilderService = prefetchFileInfoBuilderService;
            _dialogService = dialogService;
            _entriesExporter = entriesExporter;
            _messenger = messenger;

            _messenger.Register<OnSortColumnMessage>(this, HandleOnSortColumnMessage);
        }

        private bool CanExecuteLoadPrefetchInfoEntriesCommand()
            => IsBusy ? false : true;

        private async void ExecuteLoadPrefetchInfoEntriesCommandAsync()
        {
            if (PrefetchEntries != null) PrefetchEntries.Clear();
            IsBusy = true;

            try
            {
                var isAdministrator = Helper.IsAdministrator();

                if (isAdministrator)
                {
                    var getPrefetchFileInfosResult = await _prefetchFileInfoBuilderService.GetPrefetchFileInfosAsync();

                    if (getPrefetchFileInfosResult.IsSuccess)
                    {
                        PrefetchEntries = new ObservableCollection<PrefetchInfoEntry>(getPrefetchFileInfosResult.Value);

                        _messenger.Send(new OnPrefetchInfoEntriesChangedMessage(PrefetchEntries.ToList()));
                    }
                    else
                    {
                        _dialogService.ShowError(getPrefetchFileInfosResult.Error);
                    }
                }
                else
                {
                    _dialogService.ShowInfo("Per eseguire questa funzionalità occorre essere amministratori." +
                        "Riavviare l'applicazione in Modalità Amministratore.");
                } 
            }
            catch (Exception ex)
            {
                _dialogService.ShowError(ex.Message + "\n" + ex.StackTrace);
            }

            IsBusy = false;
        }

        private bool CanExecuteExportCommandAsync()
            => IsBusy == false & PrefetchEntries != null;

        private async void ExecuteExportCommandAsync()
        {
            try
            {
                var exportResult = await _entriesExporter.SaveEntriesDataAsync(PrefetchEntries, EntryType.Prefetch);

                if (exportResult.IsSuccess)
                {
                    _dialogService.ShowInfo(Activities_Inspector.Resources.ExportCommand_ExportComplete_Message);
                }
                else
                {
                    _dialogService.ShowError(exportResult.Error);
                }
            }
            catch (Exception ex)
            {
                _dialogService.ShowError(ex.Message);
            }
        }

        private void HandleOnSortColumnMessage(OnSortColumnMessage message)
        {
            if (PrefetchEntries == null || PrefetchEntries.Count == 0) return;

            var propertyType = (PrefetchPropertyType)message.NewPropertyType;
            var isAscending = message.NewIsAscending;

            if (isAscending)
            {
                switch (propertyType)
                {
                    case PrefetchPropertyType.ExecutableFileName:
                        PrefetchEntries = new ObservableCollection<PrefetchInfoEntry>(PrefetchEntries.OrderBy(d => d.ExecutableFileName));
                        break;
                    case PrefetchPropertyType.SourceFileName:
                        PrefetchEntries = new ObservableCollection<PrefetchInfoEntry>(PrefetchEntries.OrderBy(d => d.SourceFileName));
                        break;
                    case PrefetchPropertyType.Extension:
                        PrefetchEntries = new ObservableCollection<PrefetchInfoEntry>(PrefetchEntries.OrderBy(d => d.Extension));
                        break;
                    case PrefetchPropertyType.LastRunTime:
                        PrefetchEntries = new ObservableCollection<PrefetchInfoEntry>(PrefetchEntries.OrderBy(d => d.LastRunTime));
                        break;
                }
            }
            else
            {
                switch (propertyType)
                {
                    case PrefetchPropertyType.ExecutableFileName:
                        PrefetchEntries = new ObservableCollection<PrefetchInfoEntry>(PrefetchEntries.OrderByDescending(d => d.ExecutableFileName));
                        break;
                    case PrefetchPropertyType.SourceFileName:
                        PrefetchEntries = new ObservableCollection<PrefetchInfoEntry>(PrefetchEntries.OrderByDescending(d => d.SourceFileName));
                        break;
                    case PrefetchPropertyType.Extension:
                        PrefetchEntries = new ObservableCollection<PrefetchInfoEntry>(PrefetchEntries.OrderByDescending(d => d.Extension));
                        break;
                    case PrefetchPropertyType.LastRunTime:
                        PrefetchEntries = new ObservableCollection<PrefetchInfoEntry>(PrefetchEntries.OrderByDescending(d => d.LastRunTime));
                        break;
                }
            }

            _messenger.Send(new OnPrefetchInfoEntriesChangedMessage(PrefetchEntries.ToList()));
        }
    }
}
