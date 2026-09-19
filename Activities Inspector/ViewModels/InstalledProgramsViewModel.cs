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
    public class InstalledProgramsViewModel : FeatureViewModelBase<InstallEntry>
    {
        #region Proprietà

        public ObservableCollection<InstallEntry> InstallEntries
        {
            get => Entries;
            set
            {
                Entries = value;
                RaisePropertyChanged(nameof(InstallEntries));
            }
        }

        protected override void OnIsBusyChanged()
        {
            LoadInstallEntriesCommand.RaiseCanExecuteChanged();
        }

        #endregion

        #region Comandi

        private RelayCommand _loadInstallEntriesCommand;
        public RelayCommand LoadInstallEntriesCommand => _loadInstallEntriesCommand
            ?? (_loadInstallEntriesCommand = new RelayCommand(ExecuteLoadInstallEntriesCommand,
                CanExecuteLoadInstallEntriesCommand));

        #endregion

        private readonly IInstallEntriesBuilder _installEntriesBuilder;
        private readonly IMessenger _messenger;

        public InstalledProgramsViewModel(IInstallEntriesBuilder installEntriesBuilder, IDialogService dialogService,
            IEntriesExporter entriesExporter, IMessenger messenger, IAuditTrail auditTrail, Services.Evidence.IEvidenceSourceProvider sources)
            : base(dialogService, entriesExporter, auditTrail, sources)
        {
            _installEntriesBuilder = installEntriesBuilder;
            _messenger = messenger;

            _messenger.Register<OnSortColumnMessage>(this, HandleOnSortColumnMessage);
        }

        private bool CanExecuteLoadInstallEntriesCommand()
            => !IsBusy;

        private void ExecuteLoadInstallEntriesCommand()
            => Forget(LoadAsync());

        protected override EntryType EntryType => EntryType.InstalledPrograms;

        protected override async Task<Result<List<InstallEntry>>> LoadEntriesAsync(CancellationToken token)
        {
            return await _installEntriesBuilder.GetInstallEntriesAsync(token);
        }

        protected override void SetEntries(ObservableCollection<InstallEntry> entries)
        {
            InstallEntries = entries;
        }

        protected override void PublishEntries(List<InstallEntry> entries)
        {
            _messenger.Send(new OnInstallEntriesChangedMessage(entries, _installEntriesBuilder.LastIntegrityManifest));
        }

        private void HandleOnSortColumnMessage(OnSortColumnMessage message)
        {
            ApplySort(message.NewPropertyType, message.NewIsAscending);
        }
    }
}