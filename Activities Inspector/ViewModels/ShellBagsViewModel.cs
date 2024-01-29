using Activities_Inspector.Messages;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using ProgettoInformaticaForense_Argentieri.Messages;
using ProgettoInformaticaForense_Argentieri.Models;
using ProgettoInformaticaForense_Argentieri.Pages;
using ProgettoInformaticaForense_Argentieri.Services;
using ProgettoInformaticaForense_Argentieri.Utils;
using RawCopy;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;

namespace ProgettoInformaticaForense_Argentieri.ViewModels
{
    public class ShellBagsViewModel : ViewModelBase
    {
        #region Proprietà

        private ObservableCollection<ShellBagEntry> _shellBagsEntries;

        public ObservableCollection<ShellBagEntry> ShellBagsEntries
        {
            get => _shellBagsEntries;
            set
            {
                var changed = Set(nameof(ShellBagsEntries), ref _shellBagsEntries, value);

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
                    LoadShellBagsEntriesCommand.RaiseCanExecuteChanged();
                    ExportCommand.RaiseCanExecuteChanged();
                }
            }
        }

        #endregion

        #region Comandi

        private RelayCommand _loadShellBagsEntriesCommand;
        public RelayCommand LoadShellBagsEntriesCommand => _loadShellBagsEntriesCommand
            ?? (_loadShellBagsEntriesCommand = new RelayCommand(ExecuteLoadShellBagsEntriesCommandAsync,
                CanExecuteLoadShellBagsEntriesCommand));

        private RelayCommand _exportCommand;
        public RelayCommand ExportCommand => _exportCommand
            ?? (_exportCommand = new RelayCommand(ExecuteExportCommandAsync,
                CanExecuteExportCommandAsync));

        #endregion

        private readonly IShellBagsParserService _shellBagsParserService;
        private readonly IDialogService _dialogService;
        private readonly IEntriesExporter _entriesExporter;
        private readonly IMessenger _messenger;

        public ShellBagsViewModel(IShellBagsParserService shellBagsParserService, IDialogService dialogService,
            IEntriesExporter entriesExporter, IMessenger messenger)
        {
            _shellBagsParserService = shellBagsParserService;
            _dialogService = dialogService;
            _entriesExporter = entriesExporter;
            _messenger = messenger;

            _messenger.Register<OnSortColumnMessage>(this, HandleOnSortColumnMessage);
        }

        private bool CanExecuteLoadShellBagsEntriesCommand()
            => IsBusy ? false : true;

        private async void ExecuteLoadShellBagsEntriesCommandAsync()
        {
            if (ShellBagsEntries != null) ShellBagsEntries.Clear();
            IsBusy = true;

            try
            {
                var isAdministrator = Helper.IsAdministrator();

                if (isAdministrator)
                {
                    var shellbagsResult = await _shellBagsParserService.ParseShellBags();

                    if (shellbagsResult.IsSuccess)
                    {
                        var shellBags = shellbagsResult.Value;
                        var entries = GetShellBagsEntries(shellBags).Where(sb => sb.AbsolutePath != string.Empty).ToList();

                        ShellBagsEntries = new ObservableCollection<ShellBagEntry>(entries);

                        _messenger.Send(new OnShellBagEntriesChangedMessage(ShellBagsEntries.ToList()));
                    }
                    else
                    {
                        _dialogService.ShowError(shellbagsResult.Error);
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

        private bool CanExecuteExportCommandAsync()
            => IsBusy == false & ShellBagsEntries != null;

        private async void ExecuteExportCommandAsync()
        {
            try
            {
                var exportResult = await _entriesExporter.SaveEntriesDataAsync(ShellBagsEntries, EntryType.ShellBags);

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
            if (ShellBagsEntries == null || ShellBagsEntries.Count == 0) return;

            var propertyType = (ShellBagsPropertyType)message.NewPropertyType;
            var isAscending = message.NewIsAscending;

            if (isAscending)
            {
                switch (propertyType)
                {
                    case ShellBagsPropertyType.AbsolutePath:
                        ShellBagsEntries = new ObservableCollection<ShellBagEntry>(ShellBagsEntries.OrderBy(d => d.AbsolutePath));
                        break;
                    case ShellBagsPropertyType.LastRegistryWriteDate:
                        ShellBagsEntries = new ObservableCollection<ShellBagEntry>(ShellBagsEntries.OrderBy(d => d.LastRegistryWriteDate));
                        break;
                    case ShellBagsPropertyType.RegistryPath:
                        ShellBagsEntries = new ObservableCollection<ShellBagEntry>(ShellBagsEntries.OrderBy(d => d.RegistryPath));
                        break;
                }
            }
            else
            {
                switch (propertyType)
                {
                    case ShellBagsPropertyType.AbsolutePath:
                        ShellBagsEntries = new ObservableCollection<ShellBagEntry>(ShellBagsEntries.OrderByDescending(d => d.AbsolutePath));
                        break;
                    case ShellBagsPropertyType.LastRegistryWriteDate:
                        ShellBagsEntries = new ObservableCollection<ShellBagEntry>(ShellBagsEntries.OrderByDescending(d => d.LastRegistryWriteDate));
                        break;
                    case ShellBagsPropertyType.RegistryPath:
                        ShellBagsEntries = new ObservableCollection<ShellBagEntry>(ShellBagsEntries.OrderByDescending(d => d.RegistryPath));
                        break;
                }
            }

            _messenger.Send(new OnShellBagEntriesChangedMessage(ShellBagsEntries.ToList()));
        }
    }
}
