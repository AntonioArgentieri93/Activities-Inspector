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
using System.Threading;
using System.Threading.Tasks;

namespace ProgettoInformaticaForense_Argentieri.ViewModels
{
    public class RecentFolderViewModel : CancellableViewModelBase
    {
        #region Proprietà

        private ObservableCollection<RecentFolderEntry> _recentFolderEntries;

        public ObservableCollection<RecentFolderEntry> RecentFolderEntries
        {
            get => _recentFolderEntries;
            set
            {
                var changed = Set(nameof(RecentFolderEntries), ref _recentFolderEntries, value);

                if (changed)
                {
                    ExportCommand.RaiseCanExecuteChanged();
                }
            }
        }

        protected override void OnIsBusyChanged()
        {
            LoadRecentFolderEntriesCommand.RaiseCanExecuteChanged();
            ExportCommand.RaiseCanExecuteChanged();
        }

        #endregion

        #region Comandi

        private RelayCommand _loadRecentFolderEntriesCommand;
        public RelayCommand LoadRecentFolderEntriesCommand => _loadRecentFolderEntriesCommand
            ?? (_loadRecentFolderEntriesCommand = new RelayCommand(ExecuteLoadRecentFolderEntriesCommand,
                CanExecuteLoadRecentFolderEntriesCommand));

        private RelayCommand _exportCommand;
        public RelayCommand ExportCommand => _exportCommand
            ?? (_exportCommand = new RelayCommand(ExecuteExportCommand,
                CanExecuteExportCommandAsync));

        #endregion

        private readonly IRecentFilesService _recentFilesService;
        private readonly IEntriesExporter _entriesExporter;
        private readonly IMessenger _messenger;

        public RecentFolderViewModel(IRecentFilesService recentFilesService, IDialogService dialogService,
            IEntriesExporter entriesExporter, IMessenger messenger)
            : base(dialogService)
        {
            _recentFilesService = recentFilesService;
            _entriesExporter = entriesExporter;
            _messenger = messenger;

            _messenger.Register<OnSortColumnMessage>(this, HandleOnSortColumnMessage);
        }

        private bool CanExecuteLoadRecentFolderEntriesCommand()
            => !IsBusy;

        private void ExecuteLoadRecentFolderEntriesCommand()
            => Forget(LoadRecentFolderEntriesAsync());

        private async Task LoadRecentFolderEntriesAsync()
        {
            if (RecentFolderEntries != null) RecentFolderEntries.Clear();
            var token = BeginOperation();

            try
            {
                var getRecentFilesResult = await _recentFilesService.GetRecentFilesAsync(token);

                if (getRecentFilesResult.IsSuccess)
                {
                    RecentFolderEntries = new ObservableCollection<RecentFolderEntry>(getRecentFilesResult.Value);

                    _messenger.Send(new OnRecentFolderEntriesChangedMessage(RecentFolderEntries.ToList()));
                }
                else
                {
                    Dialogs.ShowError(getRecentFilesResult.Error);
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
            => !IsBusy && RecentFolderEntries != null;

        private void ExecuteExportCommand()
            => Forget(ExportAsync());

        private async Task ExportAsync()
        {
            var token = BeginOperation();

            try
            {
                var exportResult = await _entriesExporter.SaveEntriesDataAsync(RecentFolderEntries, EntryType.Recents, token);

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
            if (RecentFolderEntries == null || RecentFolderEntries.Count == 0) return;

            var propertyType = (RecentsFolderEntryPropertyType)message.NewPropertyType;
            var isAscending = message.NewIsAscending;

            if (isAscending)
            {
                switch (propertyType)
                {
                    case RecentsFolderEntryPropertyType.FileName:
                        RecentFolderEntries = new ObservableCollection<RecentFolderEntry>(RecentFolderEntries.OrderBy(d => d.FileName));
                        break;
                    case RecentsFolderEntryPropertyType.DataSource:
                        RecentFolderEntries = new ObservableCollection<RecentFolderEntry>(RecentFolderEntries.OrderBy(d => d.DataSource));
                        break;
                    case RecentsFolderEntryPropertyType.FullPath:
                        RecentFolderEntries = new ObservableCollection<RecentFolderEntry>(RecentFolderEntries.OrderBy(d => d.FullPath));
                        break;
                    case RecentsFolderEntryPropertyType.ActionTime:
                        RecentFolderEntries = new ObservableCollection<RecentFolderEntry>(RecentFolderEntries.OrderBy(d => d.ActionTime));
                        break;
                }
            }
            else
            {
                switch (propertyType)
                {
                    case RecentsFolderEntryPropertyType.FileName:
                        RecentFolderEntries = new ObservableCollection<RecentFolderEntry>(RecentFolderEntries.OrderByDescending(d => d.FileName));
                        break;
                    case RecentsFolderEntryPropertyType.DataSource:
                        RecentFolderEntries = new ObservableCollection<RecentFolderEntry>(RecentFolderEntries.OrderByDescending(d => d.DataSource));
                        break;
                    case RecentsFolderEntryPropertyType.FullPath:
                        RecentFolderEntries = new ObservableCollection<RecentFolderEntry>(RecentFolderEntries.OrderByDescending(d => d.FullPath));
                        break;
                    case RecentsFolderEntryPropertyType.ActionTime:
                        RecentFolderEntries = new ObservableCollection<RecentFolderEntry>(RecentFolderEntries.OrderByDescending(d => d.ActionTime));
                        break;
                }
            }

            _messenger.Send(new OnRecentFolderEntriesChangedMessage(RecentFolderEntries.ToList()));
        }
    }
}