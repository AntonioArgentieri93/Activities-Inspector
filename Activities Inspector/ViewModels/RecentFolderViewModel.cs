using Activities_Inspector.Messages;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using Activities_Inspector.Messages;
using Activities_Inspector.Models;
using Activities_Inspector.Pages;
using Activities_Inspector.Services;
using CSharpFunctionalExtensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Activities_Inspector.ViewModels
{
    public class RecentFolderViewModel : FeatureViewModelBase<RecentFolderEntry>
    {
        #region Proprietà

        public ObservableCollection<RecentFolderEntry> RecentFolderEntries
        {
            get => Entries;
            set
            {
                Entries = value;
                RaisePropertyChanged(nameof(RecentFolderEntries));
            }
        }

        protected override void OnIsBusyChanged()
        {
            LoadRecentFolderEntriesCommand.RaiseCanExecuteChanged();
        }

        #endregion

        #region Comandi

        private RelayCommand _loadRecentFolderEntriesCommand;
        public RelayCommand LoadRecentFolderEntriesCommand => _loadRecentFolderEntriesCommand
            ?? (_loadRecentFolderEntriesCommand = new RelayCommand(ExecuteLoadRecentFolderEntriesCommand,
                CanExecuteLoadRecentFolderEntriesCommand));

        #endregion

        private readonly IRecentFilesService _recentFilesService;
        private readonly IMessenger _messenger;

        public RecentFolderViewModel(IRecentFilesService recentFilesService, IDialogService dialogService,
            IEntriesExporter entriesExporter, IMessenger messenger)
            : base(dialogService, entriesExporter)
        {
            _recentFilesService = recentFilesService;
            _messenger = messenger;

            _messenger.Register<OnSortColumnMessage>(this, HandleOnSortColumnMessage);
        }

        private bool CanExecuteLoadRecentFolderEntriesCommand()
            => !IsBusy;

        private void ExecuteLoadRecentFolderEntriesCommand()
            => Forget(LoadAsync());

        protected override EntryType EntryType => EntryType.Recents;

        protected override async Task<Result<List<RecentFolderEntry>>> LoadEntriesAsync(CancellationToken token)
        {
            return await _recentFilesService.GetRecentFilesAsync(token);
        }

        protected override void SetEntries(ObservableCollection<RecentFolderEntry> entries)
        {
            RecentFolderEntries = entries;
        }

        protected override void PublishEntries(List<RecentFolderEntry> entries)
        {
            _messenger.Send(new OnRecentFolderEntriesChangedMessage(entries));
        }

        protected override void AfterLoad()
        {
            if (_recentFilesService.SkippedFilesCount > 0)
            {
                Dialogs.ShowInfo(
                    $"{_recentFilesService.SkippedFilesCount} file ignorati perché illeggibili.");
            }
        }

        private void HandleOnSortColumnMessage(OnSortColumnMessage message)
        {
            ApplySort(message.NewPropertyType, message.NewIsAscending);
        }
    }
}