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
    public class SessionsViewModel : CancellableViewModelBase
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

        protected override void OnIsBusyChanged()
        {
            LoadSessionEntriesCommand.RaiseCanExecuteChanged();
            ExportCommand.RaiseCanExecuteChanged();
        }

        #endregion

        #region Comandi

        private RelayCommand _loadSessionEntriesCommand;
        public RelayCommand LoadSessionEntriesCommand => _loadSessionEntriesCommand
            ?? (_loadSessionEntriesCommand = new RelayCommand(ExecuteLoadSessionEntries,
                CanExecuteExecuteLoadSessionEntriesCommand));

        private RelayCommand _exportCommand;
        public RelayCommand ExportCommand => _exportCommand
            ?? (_exportCommand = new RelayCommand(ExecuteExport,
                CanExecuteExportCommand));

        #endregion

        private readonly ILoggedInfoService _loggedInfoService;
        private readonly IEntriesExporter _entriesExporter;
        private readonly IMessenger _messenger;

        public SessionsViewModel(ILoggedInfoService loggedInfoService, IDialogService dialogService,
            IEntriesExporter entriesExporter, IMessenger messenger)
            : base(dialogService)
        {
            _loggedInfoService = loggedInfoService;
            _entriesExporter = entriesExporter;
            _messenger = messenger;

            _messenger.Register<OnSortColumnMessage>(this, HandleOnSortColumnMessage);
        }

        private bool CanExecuteExecuteLoadSessionEntriesCommand()
            => !IsBusy;

        private void ExecuteLoadSessionEntries()
            => Forget(LoadSessionEntriesAsync());

        private async Task LoadSessionEntriesAsync()
        {
            if (Sessions != null) Sessions.Clear();
            var token = BeginOperation();

            try
            {
                var isAdministrator = Helper.IsAdministrator();

                if (isAdministrator)
                {
                    var getSessionsResult = await _loggedInfoService.GetSessionsAsync(token);

                    if (getSessionsResult.IsSuccess)
                    {
                        var events = getSessionsResult.Value;
                        Sessions = new ObservableCollection<SessionEntry>(events);

                        _messenger.Send(new OnSessionEntriesChangedMessage(Sessions.ToList()));
                    }
                    else
                    {
                        Dialogs.ShowError(getSessionsResult.Error);
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

        private bool CanExecuteExportCommand()
            => !IsBusy && Sessions != null;

        private void ExecuteExport()
            => Forget(ExportAsync());

        private async Task ExportAsync()
        {
            var token = BeginOperation();

            try
            {
                var exportResult = await _entriesExporter.SaveEntriesDataAsync(Sessions, EntryType.Sessions, token);

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