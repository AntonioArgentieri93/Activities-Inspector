using Activities_Inspector.Messages;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using ProgettoInformaticaForense_Argentieri.Messages;
using ProgettoInformaticaForense_Argentieri.Models;
using ProgettoInformaticaForense_Argentieri.Pages;
using ProgettoInformaticaForense_Argentieri.Services;
using CSharpFunctionalExtensions;
using RawCopy;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ProgettoInformaticaForense_Argentieri.ViewModels
{
    public class SystemTimeChangedViewModel : FeatureViewModelBase<SystemTimeChangedEntry>
    {
        #region Proprietà

        public ObservableCollection<SystemTimeChangedEntry> TimeChangedEntries
        {
            get => Entries;
            set
            {
                Entries = value;
                RaisePropertyChanged(nameof(TimeChangedEntries));
            }
        }

        protected override void OnIsBusyChanged()
        {
            LoadSystemTimeChangedCommand.RaiseCanExecuteChanged();
        }

        #endregion

        #region Comandi

        private RelayCommand _loadSystemTimeChangedCommand;
        public RelayCommand LoadSystemTimeChangedCommand => _loadSystemTimeChangedCommand
            ?? (_loadSystemTimeChangedCommand = new RelayCommand(ExecuteLoadSystemTimeChangedCommand,
                CanExecuteLoadSystemTimeChangedCommand));

        #endregion

        private readonly ISystemTimeChangedService _timeChangedService;
        private readonly IMessenger _messenger;

        public SystemTimeChangedViewModel(ISystemTimeChangedService timeChangedService, IDialogService dialogService,
            IEntriesExporter entriesExporter, IMessenger messenger)
            : base(dialogService, entriesExporter)
        {
            _timeChangedService = timeChangedService;
            _messenger = messenger;

            _messenger.Register<OnSortColumnMessage>(this, HandleOnSortColumnMessage);
        }

        private bool CanExecuteLoadSystemTimeChangedCommand()
            => !IsBusy;

        private void ExecuteLoadSystemTimeChangedCommand()
            => Forget(LoadAsync());

        protected override EntryType EntryType => EntryType.SystemTimeChanged;
        protected override bool RequiresAdmin => true;

        protected override async Task<Result<List<SystemTimeChangedEntry>>> LoadEntriesAsync(CancellationToken token)
        {
            return await _timeChangedService.GetSystemTimeChangedEntriesAsync(token);
        }

        protected override void SetEntries(ObservableCollection<SystemTimeChangedEntry> entries)
        {
            TimeChangedEntries = entries;
        }

        protected override void PublishEntries(List<SystemTimeChangedEntry> entries)
        {
            _messenger.Send(new OnSystemTimeChangedEntriesChangedMessage(entries));
        }

        private void HandleOnSortColumnMessage(OnSortColumnMessage message)
        {
            ApplySort(message.NewPropertyType, message.NewIsAscending);
        }
    }
}