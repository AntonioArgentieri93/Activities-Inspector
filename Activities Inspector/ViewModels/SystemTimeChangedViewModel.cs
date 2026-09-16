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
    public class SystemTimeChangedViewModel : CancellableViewModelBase
    {
        #region Proprietà

        private ObservableCollection<SystemTimeChangedEntry> _timeChangedEntries;

        public ObservableCollection<SystemTimeChangedEntry> TimeChangedEntries
        {
            get => _timeChangedEntries;
            set
            {
                var changed = Set(nameof(TimeChangedEntries), ref _timeChangedEntries, value);

                if (changed)
                {
                    ExportCommand.RaiseCanExecuteChanged();
                }
            }
        }

        protected override void OnIsBusyChanged()
        {
            LoadSystemTimeChangedCommand.RaiseCanExecuteChanged();
            ExportCommand.RaiseCanExecuteChanged();
        }

        #endregion

        #region Comandi

        private RelayCommand _loadSystemTimeChangedCommand;
        public RelayCommand LoadSystemTimeChangedCommand => _loadSystemTimeChangedCommand
            ?? (_loadSystemTimeChangedCommand = new RelayCommand(ExecuteLoadSystemTimeChangedCommand,
                CanExecuteLoadSystemTimeChangedCommand));

        private RelayCommand _exportCommand;
        public RelayCommand ExportCommand => _exportCommand
            ?? (_exportCommand = new RelayCommand(ExecuteExportCommand,
                CanExecuteExportCommandAsync));

        #endregion

        private readonly ISystemTimeChangedService _timeChangedService;
        private readonly IEntriesExporter _entriesExporter;
        private readonly IMessenger _messenger;

        public SystemTimeChangedViewModel(ISystemTimeChangedService timeChangedService, IDialogService dialogService,
            IEntriesExporter entriesExporter, IMessenger messenger)
            : base(dialogService)
        {
            _timeChangedService = timeChangedService;
            _entriesExporter = entriesExporter;
            _messenger = messenger;

            _messenger.Register<OnSortColumnMessage>(this, HandleOnSortColumnMessage);
        }

        private bool CanExecuteLoadSystemTimeChangedCommand()
            => !IsBusy;

        private void ExecuteLoadSystemTimeChangedCommand()
            => Forget(LoadSystemTimeChangedAsync());

        private async Task LoadSystemTimeChangedAsync()
        {
            if (TimeChangedEntries != null) TimeChangedEntries.Clear();
            var token = BeginOperation();

            try
            {
                var isAdministrator = Helper.IsAdministrator();

                if (isAdministrator)
                {
                    var result = await _timeChangedService.GetSystemTimeChangedEntriesAsync(token);

                    if (result.IsSuccess)
                    {
                        var events = result.Value;
                        TimeChangedEntries = new ObservableCollection<SystemTimeChangedEntry>(result.Value);

                        _messenger.Send(new OnSystemTimeChangedEntriesChangedMessage(TimeChangedEntries.ToList()));
                    }
                    else
                    {
                        Dialogs.ShowError(result.Error);
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
            => !IsBusy && TimeChangedEntries != null;

        private void ExecuteExportCommand()
            => Forget(ExportAsync());

        private async Task ExportAsync()
        {
            var token = BeginOperation();

            try
            {
                var exportResult = await _entriesExporter.SaveEntriesDataAsync(TimeChangedEntries, EntryType.SystemTimeChanged, token);

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
            if (TimeChangedEntries == null || TimeChangedEntries.Count == 0) return;

            var propertyType = (SystemTimeChangedPropertyType)message.NewPropertyType;
            var isAscending = message.NewIsAscending;

            if (isAscending)
            {
                switch (propertyType)
                {
                    case SystemTimeChangedPropertyType.AccountName:
                        TimeChangedEntries = new ObservableCollection<SystemTimeChangedEntry>(TimeChangedEntries.OrderBy(d => d.AccountName));
                        break;
                    case SystemTimeChangedPropertyType.TimeGenerated:
                        TimeChangedEntries = new ObservableCollection<SystemTimeChangedEntry>(TimeChangedEntries.OrderBy(d => d.TimeGenerated));
                        break;
                    case SystemTimeChangedPropertyType.OldTime:
                        TimeChangedEntries = new ObservableCollection<SystemTimeChangedEntry>(TimeChangedEntries.OrderBy(d => d.OldTime));
                        break;
                    case SystemTimeChangedPropertyType.NewTime:
                        TimeChangedEntries = new ObservableCollection<SystemTimeChangedEntry>(TimeChangedEntries.OrderBy(d => d.NewTime));
                        break;
                }
            }
            else
            {
                switch (propertyType)
                {
                    case SystemTimeChangedPropertyType.AccountName:
                        TimeChangedEntries = new ObservableCollection<SystemTimeChangedEntry>(TimeChangedEntries.OrderByDescending(d => d.AccountName));
                        break;
                    case SystemTimeChangedPropertyType.TimeGenerated:
                        TimeChangedEntries = new ObservableCollection<SystemTimeChangedEntry>(TimeChangedEntries.OrderByDescending(d => d.TimeGenerated));
                        break;
                    case SystemTimeChangedPropertyType.OldTime:
                        TimeChangedEntries = new ObservableCollection<SystemTimeChangedEntry>(TimeChangedEntries.OrderByDescending(d => d.OldTime));
                        break;
                    case SystemTimeChangedPropertyType.NewTime:
                        TimeChangedEntries = new ObservableCollection<SystemTimeChangedEntry>(TimeChangedEntries.OrderByDescending(d => d.NewTime));
                        break;
                }
            }

            _messenger.Send(new OnSystemTimeChangedEntriesChangedMessage(TimeChangedEntries.ToList()));
        }
    }
}