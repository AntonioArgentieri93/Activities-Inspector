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
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

namespace ProgettoInformaticaForense_Argentieri.ViewModels
{
    public class ShellBagsViewModel : CancellableViewModelBase
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

        protected override void OnIsBusyChanged()
        {
            LoadShellBagsEntriesCommand.RaiseCanExecuteChanged();
            ExportCommand.RaiseCanExecuteChanged();
        }

        #endregion

        #region Comandi

        private RelayCommand _loadShellBagsEntriesCommand;
        public RelayCommand LoadShellBagsEntriesCommand => _loadShellBagsEntriesCommand
            ?? (_loadShellBagsEntriesCommand = new RelayCommand(ExecuteLoadShellBagsEntriesCommand,
                CanExecuteLoadShellBagsEntriesCommand));

        private RelayCommand _exportCommand;
        public RelayCommand ExportCommand => _exportCommand
            ?? (_exportCommand = new RelayCommand(ExecuteExportCommand,
                CanExecuteExportCommandAsync));

        #endregion

        private readonly IShellBagsParserService _shellBagsParserService;
        private readonly IEntriesExporter _entriesExporter;
        private readonly IMessenger _messenger;

        public ShellBagsViewModel(IShellBagsParserService shellBagsParserService, IDialogService dialogService,
            IEntriesExporter entriesExporter, IMessenger messenger)
            : base(dialogService)
        {
            _shellBagsParserService = shellBagsParserService;
            _entriesExporter = entriesExporter;
            _messenger = messenger;

            _messenger.Register<OnSortColumnMessage>(this, HandleOnSortColumnMessage);
        }

        private bool CanExecuteLoadShellBagsEntriesCommand()
            => !IsBusy;

        private void ExecuteLoadShellBagsEntriesCommand()
            => Forget(LoadShellBagsEntriesAsync());

        private async Task LoadShellBagsEntriesAsync()
        {
            if (ShellBagsEntries != null) ShellBagsEntries.Clear();
            var token = BeginOperation();

            try
            {
                var isAdministrator = Helper.IsAdministrator();

                if (isAdministrator)
                {
                    var shellbagsResult = await _shellBagsParserService.ParseShellBagsAsync(token);

                    if (shellbagsResult.IsSuccess)
                    {
                        var shellBags = shellbagsResult.Value;
                        var entries = GetShellBagsEntries(shellBags)
                            .Where(sb => sb.AbsolutePath != string.Empty)
                            .ToList();

                        ShellBagsEntries = new ObservableCollection<ShellBagEntry>(entries);

                        _messenger.Send(new OnShellBagEntriesChangedMessage(ShellBagsEntries.ToList()));
                    }
                    else
                    {
                        Dialogs.ShowError(shellbagsResult.Error);
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

        private bool CanExecuteExportCommandAsync()
            => !IsBusy && ShellBagsEntries != null;

        private void ExecuteExportCommand()
            => Forget(ExportAsync());

        private async Task ExportAsync()
        {
            var token = BeginOperation();

            try
            {
                var exportResult = await _entriesExporter.SaveEntriesDataAsync(ShellBagsEntries, EntryType.ShellBags, token);

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