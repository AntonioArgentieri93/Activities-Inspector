using Activities_Inspector.Messages;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using ProgettoInformaticaForense_Argentieri.Messages;
using ProgettoInformaticaForense_Argentieri.Models;
using ProgettoInformaticaForense_Argentieri.Pages;
using ProgettoInformaticaForense_Argentieri.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace ProgettoInformaticaForense_Argentieri.ViewModels
{
    public class SystemTimeChangedViewModel : ViewModelBase
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

        private bool _isBusy;

        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                var changed = Set(nameof(IsBusy), ref _isBusy, value);

                if (changed)
                {
                    LoadSystemTimeChangedCommand.RaiseCanExecuteChanged();
                    ExportCommand.RaiseCanExecuteChanged();
                }
            }
        }

        #endregion

        #region Comandi

        private RelayCommand _loadSystemTimeChangedCommand;
        public RelayCommand LoadSystemTimeChangedCommand => _loadSystemTimeChangedCommand
            ?? (_loadSystemTimeChangedCommand = new RelayCommand(ExecuteLoadSystemTimeChangedCommandAsync,
                CanExecuteLoadSystemTimeChangedCommand));

        private RelayCommand _exportCommand;
        public RelayCommand ExportCommand => _exportCommand
            ?? (_exportCommand = new RelayCommand(ExecuteExportCommandAsync,
                CanExecuteExportCommandAsync));

        #endregion

        private readonly ISystemTimeChangedService _timeChanged;
        private readonly IDialogService _dialogService;
        private readonly IEntriesExporter _entriesExporter;
        private readonly IMessenger _messenger;

        public SystemTimeChangedViewModel(ISystemTimeChangedService timeChanged, IDialogService dialogService,
            IEntriesExporter entriesExporter, IMessenger messenger)
        {
            _timeChanged = timeChanged;
            _dialogService = dialogService;
            _entriesExporter = entriesExporter;
            _messenger = messenger;

            _messenger.Register<OnSortColumnMessage>(this, HandleOnSortColumnMessage);
        }

        private bool CanExecuteLoadSystemTimeChangedCommand()
            => IsBusy ? false : true;

        private async void ExecuteLoadSystemTimeChangedCommandAsync()
        {
            if (TimeChangedEntries != null) TimeChangedEntries.Clear();
            IsBusy = true;

            try
            {
                var result = await _timeChanged.GetSystemTimeChangedEntriesAsync();

                if (result.IsSuccess)
                {
                    var events = result.Value;
                    TimeChangedEntries = new ObservableCollection<SystemTimeChangedEntry>(result.Value);

                    _messenger.Send(new OnSystemTimeChangedEntriesChangedMessage(TimeChangedEntries.ToList()));
                }
            }
            catch (Exception ex)
            {
                _dialogService.ShowError(ex.Message + "\n" + ex.StackTrace);
            }

            IsBusy = false;
        }

        private bool CanExecuteExportCommandAsync()
            => IsBusy == false & TimeChangedEntries != null;

        private async void ExecuteExportCommandAsync()
        {
            try
            {
                var exportResult = await _entriesExporter.SaveEntriesDataAsync(TimeChangedEntries, EntryType.SystemTimeChanged);

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
