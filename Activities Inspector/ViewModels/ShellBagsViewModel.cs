using Activities_Inspector.Messages;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using Activities_Inspector.Messages;
using Activities_Inspector.Models;
using Activities_Inspector.Pages;
using Activities_Inspector.Services;
using CSharpFunctionalExtensions;
using Activities_Inspector.Utils;
using RawCopy;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

namespace Activities_Inspector.ViewModels
{
    public class ShellBagsViewModel : FeatureViewModelBase<ShellBagEntry>
    {
        #region Proprietà

        public ObservableCollection<ShellBagEntry> ShellBagsEntries
        {
            get => Entries;
            set
            {
                Entries = value;
                RaisePropertyChanged(nameof(ShellBagsEntries));
            }
        }

        protected override void OnIsBusyChanged()
        {
            LoadShellBagsEntriesCommand.RaiseCanExecuteChanged();
        }

        #endregion

        #region Comandi

        private RelayCommand _loadShellBagsEntriesCommand;
        public RelayCommand LoadShellBagsEntriesCommand => _loadShellBagsEntriesCommand
            ?? (_loadShellBagsEntriesCommand = new RelayCommand(ExecuteLoadShellBagsEntriesCommand,
                CanExecuteLoadShellBagsEntriesCommand));

        #endregion

        private readonly IShellBagsParserService _shellBagsParserService;
        private readonly IMessenger _messenger;

        public ShellBagsViewModel(IShellBagsParserService shellBagsParserService, IDialogService dialogService,
            IEntriesExporter entriesExporter, IMessenger messenger)
            : base(dialogService, entriesExporter)
        {
            _shellBagsParserService = shellBagsParserService;
            _messenger = messenger;

            _messenger.Register<OnSortColumnMessage>(this, HandleOnSortColumnMessage);
        }

        private bool CanExecuteLoadShellBagsEntriesCommand()
            => !IsBusy;

        private void ExecuteLoadShellBagsEntriesCommand()
            => Forget(LoadAsync());

        protected override EntryType EntryType => EntryType.ShellBags;
        protected override bool RequiresAdmin => true;

        protected override async Task<Result<List<ShellBagEntry>>> LoadEntriesAsync(CancellationToken token)
        {
            var shellbagsResult = await _shellBagsParserService.ParseShellBagsAsync(token);

            if (!shellbagsResult.IsSuccess)
                return Result.Failure<List<ShellBagEntry>>(shellbagsResult.Error);

            return Result.Success(GetShellBagsEntries(shellbagsResult.Value)
                .Where(sb => sb.AbsolutePath != string.Empty)
                .ToList());
        }

        protected override void SetEntries(ObservableCollection<ShellBagEntry> entries)
        {
            ShellBagsEntries = entries;
        }

        protected override void PublishEntries(List<ShellBagEntry> entries)
        {
            _messenger.Send(new OnShellBagEntriesChangedMessage(entries));
        }

        private static IEnumerable<ShellBagEntry> GetShellBagsEntries(List<IShellItem> shellBags)
        {
            foreach (var item in shellBags)
            {
                var properties = item.GetAllProperties();

                var absPath = properties.ContainsKey("AbsolutePath") ? properties["AbsolutePath"] : string.Empty;
                var lrwDate = properties.ContainsKey("LastRegistryWriteDate") ? DateBuilder.ConvertToLocalDate(properties["LastRegistryWriteDate"]) : DateTime.MinValue;
                var regPath = properties.ContainsKey("RegistryPath") ? properties["RegistryPath"] : string.Empty;

                yield return new ShellBagEntry(absPath, lrwDate, regPath);
            }
        }

        private void HandleOnSortColumnMessage(OnSortColumnMessage message)
        {
            ApplySort(message.NewPropertyType, message.NewIsAscending);
        }
    }
}