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
    public class SessionsViewModel : ViewModelBase
    {
        #region Proprietà

        private ObservableCollection<SessionEntry> _sessions;

        public ObservableCollection<SessionEntry> Sessions
        {
            get => _sessions;
            set
            {
                var changed = Set(nameof(Sessions), ref _sessions, value);

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
                    LoadSessionEntriesCommand.RaiseCanExecuteChanged();
                    ExportCommand.RaiseCanExecuteChanged();
                }
            }
        }

        #endregion

        #region Comandi

        private RelayCommand _loadSessionEntriesCommand;
        public RelayCommand LoadSessionEntriesCommand => _loadSessionEntriesCommand
            ?? (_loadSessionEntriesCommand = new RelayCommand(ExecuteLoadSessionEntriesCommand,
                CanExecuteExecuteLoadSessionEntriesCommand));

        private RelayCommand _exportCommand;
        public RelayCommand ExportCommand => _exportCommand
            ?? (_exportCommand = new RelayCommand(ExecuteExportCommand,
                CanExecuteExportCommand));

        #endregion

        private readonly ILoggedInfoService _loggedInfoService;
        private readonly IDialogService _dialogService;
        private readonly IEntriesExporter _entriesExporter;
        private readonly IMessenger _messenger;

        public SessionsViewModel(ILoggedInfoService loggedInfoService, IDialogService dialogService,
            IEntriesExporter entriesExporter, IMessenger messenger)
        {
            _loggedInfoService = loggedInfoService;
            _dialogService = dialogService;
            _entriesExporter = entriesExporter;
            _messenger = messenger;

            _messenger.Register<OnSortColumnMessage>(this, HandleOnSortColumnMessage);
        }


        private bool CanExecuteExecuteLoadSessionEntriesCommand()
            => IsBusy ? false : true;

        private async void ExecuteLoadSessionEntriesCommand()
        {
            if (Sessions != null) Sessions.Clear();
            IsBusy = true;

            try
            {
                var isAdministrator = Helper.IsAdministrator();

                if (isAdministrator)
                {
                    var getSessionsResult = await _loggedInfoService.GetSessionsAsync();

                    if (getSessionsResult.IsSuccess)
                    {
                        var events = getSessionsResult.Value;
                        Sessions = new ObservableCollection<SessionEntry>(events);

                        _messenger.Send(new OnSessionEntriesChangedMessage(Sessions.ToList()));
                    }
                    else
                    {
                        _dialogService.ShowError(getSessionsResult.Error);
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

        private bool CanExecuteExportCommand()
            => IsBusy == false & Sessions != null;

        private async void ExecuteExportCommand()
        {
            try
            {
                var exportResult = await _entriesExporter.SaveEntriesDataAsync(Sessions, EntryType.Sessions);

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
            if (Sessions == null || Sessions.Count == 0) return;

            var propertyType = (SessionPropertyType)message.NewPropertyType;
            var isAscending = message.NewIsAscending;

            if (isAscending)
            {
                switch (propertyType)
                {
                    case SessionPropertyType.UserName:
                        Sessions = new ObservableCollection<SessionEntry>(Sessions.OrderBy(d => d.UserName));
                        break;
                    case SessionPropertyType.Group:
                        Sessions = new ObservableCollection<SessionEntry>(Sessions.OrderBy(d => d.Group));
                        break;
                    case SessionPropertyType.MachineName:
                        Sessions = new ObservableCollection<SessionEntry>(Sessions.OrderBy(d => d.MachineName));
                        break;
                    case SessionPropertyType.LogOnTime:
                        Sessions = new ObservableCollection<SessionEntry>(Sessions.OrderBy(d => d.LogOnTime));
                        break;
                    case SessionPropertyType.LogOffTime:
                        Sessions = new ObservableCollection<SessionEntry>(Sessions.OrderBy(d => d.LogOffTime));
                        break;
                    case SessionPropertyType.Duration:
                        Sessions = new ObservableCollection<SessionEntry>(Sessions.OrderBy(d => d.Duration));
                        break;
                    case SessionPropertyType.NetworkAddress:
                        Sessions = new ObservableCollection<SessionEntry>(Sessions.OrderBy(d => d.NetworkAddress));
                        break;
                    case SessionPropertyType.AccessType:
                        Sessions = new ObservableCollection<SessionEntry>(Sessions.OrderBy(d => d.AccessType));
                        break;
                }
            }
            else
            {
                switch (propertyType)
                {
                    case SessionPropertyType.UserName:
                        Sessions = new ObservableCollection<SessionEntry>(Sessions.OrderByDescending(d => d.UserName));
                        break;
                    case SessionPropertyType.Group:
                        Sessions = new ObservableCollection<SessionEntry>(Sessions.OrderByDescending(d => d.Group));
                        break;
                    case SessionPropertyType.MachineName:
                        Sessions = new ObservableCollection<SessionEntry>(Sessions.OrderByDescending(d => d.MachineName));
                        break;
                    case SessionPropertyType.LogOnTime:
                        Sessions = new ObservableCollection<SessionEntry>(Sessions.OrderByDescending(d => d.LogOnTime));
                        break;
                    case SessionPropertyType.LogOffTime:
                        Sessions = new ObservableCollection<SessionEntry>(Sessions.OrderByDescending(d => d.LogOffTime));
                        break;
                    case SessionPropertyType.Duration:
                        Sessions = new ObservableCollection<SessionEntry>(Sessions.OrderByDescending(d => d.Duration));
                        break;
                    case SessionPropertyType.NetworkAddress:
                        Sessions = new ObservableCollection<SessionEntry>(Sessions.OrderByDescending(d => d.NetworkAddress));
                        break;
                    case SessionPropertyType.AccessType:
                        Sessions = new ObservableCollection<SessionEntry>(Sessions.OrderByDescending(d => d.AccessType));
                        break;
                }
            }

            _messenger.Send(new OnSessionEntriesChangedMessage(Sessions.ToList()));
        }
    }
}
