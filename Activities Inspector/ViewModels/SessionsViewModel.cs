using Activities_Inspector.Messages;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using Activities_Inspector.Messages;
using Activities_Inspector.Models;
using Activities_Inspector.Pages;
using Activities_Inspector.Services;
using CSharpFunctionalExtensions;
using RawCopy;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Activities_Inspector.ViewModels
{
    public class SessionsViewModel : FeatureViewModelBase<SessionEntry>
    {
        #region Proprietà

        public ObservableCollection<SessionEntry> Sessions
        {
            get => Entries;
            set
            {
                Entries = value;
                RaisePropertyChanged(nameof(Sessions));
            }
        }

        protected override void OnIsBusyChanged()
        {
            LoadSessionEntriesCommand.RaiseCanExecuteChanged();
        }

        #endregion

        #region Comandi

        private RelayCommand _loadSessionEntriesCommand;
        public RelayCommand LoadSessionEntriesCommand => _loadSessionEntriesCommand
            ?? (_loadSessionEntriesCommand = new RelayCommand(ExecuteLoadSessionEntries,
                CanExecuteExecuteLoadSessionEntriesCommand));

        #endregion

        private readonly ILoggedInfoService _loggedInfoService;
        private readonly IMessenger _messenger;

        public SessionsViewModel(ILoggedInfoService loggedInfoService, IDialogService dialogService,
            IEntriesExporter entriesExporter, IMessenger messenger)
            : base(dialogService, entriesExporter)
        {
            _loggedInfoService = loggedInfoService;
            _messenger = messenger;

            _messenger.Register<OnSortColumnMessage>(this, HandleOnSortColumnMessage);
        }

        private bool CanExecuteExecuteLoadSessionEntriesCommand()
            => !IsBusy;

        private void ExecuteLoadSessionEntries()
            => Forget(LoadAsync());

        protected override EntryType EntryType => EntryType.Sessions;
        protected override bool RequiresAdmin => true;

        protected override async Task<Result<List<SessionEntry>>> LoadEntriesAsync(CancellationToken token)
        {
            return await _loggedInfoService.GetSessionsAsync(token);
        }

        protected override void SetEntries(ObservableCollection<SessionEntry> entries)
        {
            Sessions = entries;
        }

        protected override void PublishEntries(List<SessionEntry> entries)
        {
            _messenger.Send(new OnSessionEntriesChangedMessage(entries));
        }

        private void HandleOnSortColumnMessage(OnSortColumnMessage message)
        {
            ApplySort(message.NewPropertyType, message.NewIsAscending);
        }
    }
}