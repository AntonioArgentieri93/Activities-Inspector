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
    public class InstalledProgramsViewModel : CancellableViewModelBase
    {
        #region Proprietà

        private ObservableCollection<InstallEntry> _installEntries;

        public ObservableCollection<InstallEntry> InstallEntries
        {
            get => _installEntries;
            set
            {
                var changed = Set(nameof(InstallEntries), ref _installEntries, value);

                if (changed)
                {
                    ExportCommand.RaiseCanExecuteChanged();
                }
            }
        }

        protected override void OnIsBusyChanged()
        {
            LoadInstallEntriesCommand.RaiseCanExecuteChanged();
            ExportCommand.RaiseCanExecuteChanged();
        }

        #endregion

        #region Comandi

        private RelayCommand _loadInstallEntriesCommand;
        public RelayCommand LoadInstallEntriesCommand => _loadInstallEntriesCommand
            ?? (_loadInstallEntriesCommand = new RelayCommand(ExecuteLoadInstallEntriesCommand,
                CanExecuteLoadInstallEntriesCommand));

        private RelayCommand _exportCommand;
        public RelayCommand ExportCommand => _exportCommand
            ?? (_exportCommand = new RelayCommand(ExecuteExportCommand,
                CanExecuteExportCommandAsync));

        #endregion

        private readonly IInstallEntriesBuilder _installEntriesBuilder;
        private readonly IEntriesExporter _entriesExporter;
        private readonly IMessenger _messenger;

        public InstalledProgramsViewModel(IInstallEntriesBuilder installEntriesBuilder, IDialogService dialogService,
            IEntriesExporter entriesExporter, IMessenger messenger)
            : base(dialogService)
        {
            _installEntriesBuilder = installEntriesBuilder;
            _entriesExporter = entriesExporter;
            _messenger = messenger;

            _messenger.Register<OnSortColumnMessage>(this, HandleOnSortColumnMessage);
        }

        private bool CanExecuteLoadInstallEntriesCommand()
            => !IsBusy;

        private void ExecuteLoadInstallEntriesCommand()
            => Forget(LoadInstallEntriesAsync());

        private async Task LoadInstallEntriesAsync()
        {
            if (InstallEntries != null) InstallEntries.Clear();
            var token = BeginOperation();

            try
            {
                var getInstallEntriesResult = await _installEntriesBuilder.GetInstallEntriesAsync(token);

                if (getInstallEntriesResult.IsSuccess)
                {
                    InstallEntries = new ObservableCollection<InstallEntry>(getInstallEntriesResult.Value);

                    _messenger.Send(new OnInstallEntriesChangedMessage(InstallEntries.ToList()));
                }
                else
                {
                    Dialogs.ShowError(getInstallEntriesResult.Error);
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
            => !IsBusy && InstallEntries != null;

        private void ExecuteExportCommand()
            => Forget(ExportAsync());

        private async Task ExportAsync()
        {
            var token = BeginOperation();

            try
            {
                var exportResult = await _entriesExporter.SaveEntriesDataAsync(InstallEntries, EntryType.InstalledPrograms, token);

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
            if (InstallEntries == null || InstallEntries.Count == 0) return;

            var propertyType = (InstalledProgramsPropertyType)message.NewPropertyType;
            var isAscending = message.NewIsAscending;

            if (isAscending)
            {
                switch (propertyType)
                {
                    case InstalledProgramsPropertyType.FileName:
                        InstallEntries = new ObservableCollection<InstallEntry>(InstallEntries.OrderBy(d => d.FileName));
                        break;
                    case InstalledProgramsPropertyType.DataSource:
                        InstallEntries = new ObservableCollection<InstallEntry>(InstallEntries.OrderBy(d => d.DataSource));
                        break;
                    case InstalledProgramsPropertyType.FullPath:
                        InstallEntries = new ObservableCollection<InstallEntry>(InstallEntries.OrderBy(d => d.FullPath));
                        break;
                    case InstalledProgramsPropertyType.InstallDate:
                        InstallEntries = new ObservableCollection<InstallEntry>(InstallEntries.OrderBy(d => d.InstallDate));
                        break;
                }
            }
            else
            {
                switch (propertyType)
                {
                    case InstalledProgramsPropertyType.FileName:
                        InstallEntries = new ObservableCollection<InstallEntry>(InstallEntries.OrderByDescending(d => d.FileName));
                        break;
                    case InstalledProgramsPropertyType.DataSource:
                        InstallEntries = new ObservableCollection<InstallEntry>(InstallEntries.OrderByDescending(d => d.DataSource));
                        break;
                    case InstalledProgramsPropertyType.FullPath:
                        InstallEntries = new ObservableCollection<InstallEntry>(InstallEntries.OrderByDescending(d => d.FullPath));
                        break;
                    case InstalledProgramsPropertyType.InstallDate:
                        InstallEntries = new ObservableCollection<InstallEntry>(InstallEntries.OrderByDescending(d => d.InstallDate));
                        break;
                }
            }

            _messenger.Send(new OnInstallEntriesChangedMessage(InstallEntries.ToList()));
        }
    }
}