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
using System.Threading;
using System.Threading.Tasks;

namespace ProgettoInformaticaForense_Argentieri.ViewModels
{
    public class PrefetchViewModel : CancellableViewModelBase
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

        protected override void OnIsBusyChanged()
        {
            LoadPrefetchInfoEntriesCommand.RaiseCanExecuteChanged();
            ExportCommand.RaiseCanExecuteChanged();
        }

        #endregion

        #region Comandi

        private RelayCommand _loadPrefetchInfoEntriesCommand;
        public RelayCommand LoadPrefetchInfoEntriesCommand => _loadPrefetchInfoEntriesCommand
            ?? (_loadPrefetchInfoEntriesCommand = new RelayCommand(ExecuteLoadPrefetchInfoEntriesCommand,
                CanExecuteLoadPrefetchInfoEntriesCommand));

        private RelayCommand _exportCommand;
        public RelayCommand ExportCommand => _exportCommand
            ?? (_exportCommand = new RelayCommand(ExecuteExportCommand,
                CanExecuteExportCommandAsync));

        #endregion

        private readonly IPrefetchFileInfoBuilderService _prefetchFileInfoBuilderService;
        private readonly IEntriesExporter _entriesExporter;
        private readonly IMessenger _messenger;

        public PrefetchViewModel(IPrefetchFileInfoBuilderService prefetchFileInfoBuilderService,
            IDialogService dialogService, IEntriesExporter entriesExporter, IMessenger messenger)
            : base(dialogService)
        {
            _prefetchFileInfoBuilderService = prefetchFileInfoBuilderService;
            _entriesExporter = entriesExporter;
            _messenger = messenger;

            _messenger.Register<OnSortColumnMessage>(this, HandleOnSortColumnMessage);
        }

        private bool CanExecuteLoadPrefetchInfoEntriesCommand()
            => !IsBusy;

        private void ExecuteLoadPrefetchInfoEntriesCommand()
            => Forget(LoadPrefetchInfoEntriesAsync());

        private async Task LoadPrefetchInfoEntriesAsync()
        {
            if (PrefetchEntries != null) PrefetchEntries.Clear();
            var token = BeginOperation();

            try
            {
                var isAdministrator = Helper.IsAdministrator();

                if (isAdministrator)
                {
                    var getPrefetchFileInfosResult = await _prefetchFileInfoBuilderService.GetPrefetchFileInfosAsync(token);

                    if (getPrefetchFileInfosResult.IsSuccess)
                    {
                        PrefetchEntries = new ObservableCollection<PrefetchInfoEntry>(getPrefetchFileInfosResult.Value);

                        _messenger.Send(new OnPrefetchInfoEntriesChangedMessage(PrefetchEntries.ToList()));
                    }
                    else
                    {
                        Dialogs.ShowError(getPrefetchFileInfosResult.Error);
                    }
                }
                else
                {
                    Dialogs.ShowInfo("Per eseguire questa funzionalità occorre essere amministratori. " +
                        "Riavviare l'applicazione in Modalità Amministratore.");
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                Dialogs.ShowError(ex.ToString());
            }
            finally
            {
                EndOperation();
            }
        }

        private bool CanExecuteExportCommandAsync()
            => !IsBusy && PrefetchEntries != null;

        private void ExecuteExportCommand()
            => Forget(ExportAsync());

        private async Task ExportAsync()
        {
            var token = BeginOperation();

            try
            {
                var exportResult = await _entriesExporter.SaveEntriesDataAsync(PrefetchEntries, EntryType.Prefetch, token);

                if (exportResult.IsSuccess)
                {
                    Dialogs.ShowInfo(Activities_Inspector.Resources.ExportCommand_ExportComplete_Message);
                }
                else
                {
                    Dialogs.ShowError(exportResult.Error);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                Dialogs.ShowError(ex.ToString());
            }
            finally
            {
                EndOperation();
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