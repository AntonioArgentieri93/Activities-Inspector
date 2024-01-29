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
    public class RecentFolderViewModel : ViewModelBase
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

        private bool _isBusy;

        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                var changed = Set(nameof(IsBusy), ref _isBusy, value);

                if (changed)
                {
                    LoadRecentFolderEntriesCommand.RaiseCanExecuteChanged();
                    ExportCommand.RaiseCanExecuteChanged();
                }
            }
        }

        #endregion

        #region Comandi

        private RelayCommand _loadRecentFolderEntriesCommand;
        public RelayCommand LoadRecentFolderEntriesCommand => _loadRecentFolderEntriesCommand
            ?? (_loadRecentFolderEntriesCommand = new RelayCommand(ExecuteLoadRecentFolderEntriesCommandAsync,
                CanExecuteLoadRecentFolderEntriesCommand));

        private RelayCommand _exportCommand;
        public RelayCommand ExportCommand => _exportCommand
            ?? (_exportCommand = new RelayCommand(ExecuteExportCommandAsync,
                CanExecuteExportCommandAsync));

        #endregion

        private readonly IRecentFilesService _recentFilesService;
        private readonly IDialogService _dialogService;
        private readonly IEntriesExporter _entriesExporter;
        private readonly IMessenger _messenger;

        public RecentFolderViewModel(IRecentFilesService recentFilesService, IDialogService dialogService,
            IEntriesExporter entriesExporter, IMessenger messenger)
        {
            _recentFilesService = recentFilesService;
            _dialogService = dialogService;
            _entriesExporter = entriesExporter;
            _messenger = messenger;

            _messenger.Register<OnSortColumnMessage>(this, HandleOnSortColumnMessage);
        }

        private bool CanExecuteLoadRecentFolderEntriesCommand()
            => IsBusy ? false : true;

        private async void ExecuteLoadRecentFolderEntriesCommandAsync()
        {
            if (RecentFolderEntries != null) RecentFolderEntries.Clear();
            IsBusy = true;

            try
            {
                var getRecentFilesResult = await _recentFilesService.GetRecentFiles();

                if (getRecentFilesResult.IsSuccess)
                {
                    RecentFolderEntries = new ObservableCollection<RecentFolderEntry>(getRecentFilesResult.Value);

                    _messenger.Send(new OnRecentFolderEntriesChangedMessage(RecentFolderEntries.ToList()));
                }
                else
                {
                    _dialogService.ShowError(getRecentFilesResult.Error);
                }
            }
            catch (Exception ex)
            {
                _dialogService.ShowError(ex.Message + "\n" + ex.StackTrace);
            }

            IsBusy = false;
        }

        private bool CanExecuteExportCommandAsync()
            => IsBusy == false & RecentFolderEntries != null;

        private async void ExecuteExportCommandAsync()
        {
            try
            {
                var exportResult = await _entriesExporter.SaveEntriesDataAsync(RecentFolderEntries, EntryType.Recents);

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
