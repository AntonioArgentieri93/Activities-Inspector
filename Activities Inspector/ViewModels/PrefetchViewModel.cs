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
    public class PrefetchViewModel : FeatureViewModelBase<PrefetchInfoEntry>
    {
        #region Proprietà

        public ObservableCollection<PrefetchInfoEntry> PrefetchEntries
        {
            get => Entries;
            set
            {
                Entries = value;
                RaisePropertyChanged(nameof(PrefetchEntries));
            }
        }

        protected override void OnIsBusyChanged()
        {
            LoadPrefetchInfoEntriesCommand.RaiseCanExecuteChanged();
        }

        #endregion

        #region Comandi

        private RelayCommand _loadPrefetchInfoEntriesCommand;
        public RelayCommand LoadPrefetchInfoEntriesCommand => _loadPrefetchInfoEntriesCommand
            ?? (_loadPrefetchInfoEntriesCommand = new RelayCommand(ExecuteLoadPrefetchInfoEntriesCommand,
                CanExecuteLoadPrefetchInfoEntriesCommand));

        #endregion

        private readonly IPrefetchFileInfoBuilderService _prefetchFileInfoBuilderService;
        private readonly IMessenger _messenger;

        public PrefetchViewModel(IPrefetchFileInfoBuilderService prefetchFileInfoBuilderService,
            IDialogService dialogService, IEntriesExporter entriesExporter, IMessenger messenger)
            : base(dialogService, entriesExporter)
        {
            _prefetchFileInfoBuilderService = prefetchFileInfoBuilderService;
            _messenger = messenger;

            _messenger.Register<OnSortColumnMessage>(this, HandleOnSortColumnMessage);
        }

        private bool CanExecuteLoadPrefetchInfoEntriesCommand()
            => !IsBusy;

        private void ExecuteLoadPrefetchInfoEntriesCommand()
            => Forget(LoadAsync());

        protected override EntryType EntryType => EntryType.Prefetch;
        protected override bool RequiresAdmin => true;

        protected override async Task<Result<List<PrefetchInfoEntry>>> LoadEntriesAsync(CancellationToken token)
        {
            return await _prefetchFileInfoBuilderService.GetPrefetchFileInfosAsync(token);
        }

        protected override void SetEntries(ObservableCollection<PrefetchInfoEntry> entries)
        {
            PrefetchEntries = entries;
        }

        protected override void PublishEntries(List<PrefetchInfoEntry> entries)
        {
            _messenger.Send(new OnPrefetchInfoEntriesChangedMessage(entries));
        }

        private void HandleOnSortColumnMessage(OnSortColumnMessage message)
        {
            ApplySort(message.NewPropertyType, message.NewIsAscending);
        }
    }
}