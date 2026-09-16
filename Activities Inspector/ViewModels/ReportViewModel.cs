using Activities_Inspector.Models;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using ProgettoInformaticaForense_Argentieri.Messages;
using ProgettoInformaticaForense_Argentieri.Models;
using ProgettoInformaticaForense_Argentieri.Services;
using ProgettoInformaticaForense_Argentieri.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ProgettoInformaticaForense_Argentieri.ViewModels
{
    public class ReportViewModel : CancellableViewModelBase
    {
        #region Proprietà

        private string _inquirerName;

        public string InquirerName
        {
            get => _inquirerName;
            set 
            {
                var changed = Set(nameof(_inquirerName), ref _inquirerName, value);

                if (changed)
                {
                    Validate(() => ValidationUtils.IsValidStringInput(value), nameof(InquirerName));
                    GenerateReportCommand.RaiseCanExecuteChanged();
                }
            } 
        }

        private string _inquirerSurname;

        public string InquirerSurname
        {
            get => _inquirerSurname;
            set 
            {
                var changed = Set(nameof(_inquirerSurname), ref _inquirerSurname, value);

                if (changed)
                {
                    Validate(() => ValidationUtils.IsValidStringInput(value), nameof(InquirerSurname));
                    GenerateReportCommand.RaiseCanExecuteChanged();
                }
            } 
        }

        private string _inquirerQualification;

        public string InquirerQualification
        {
            get => _inquirerQualification;
            set 
            {
                var changed = Set(nameof(_inquirerQualification), ref _inquirerQualification, value);

                if (changed)
                {
                    Validate(() => ValidationUtils.IsValidStringInput(value), nameof(InquirerQualification));
                    GenerateReportCommand.RaiseCanExecuteChanged();
                }
            } 
        }

        private string _objectDescription;

        public string ObjectDescription
        {
            get => _objectDescription;
            set 
            {
                var changed = Set(nameof(_objectDescription), ref _objectDescription, value);

                if (changed)
                {
                    Validate(() => ValidationUtils.IsValidStringInput(value), nameof(ObjectDescription));
                    GenerateReportCommand.RaiseCanExecuteChanged();
                }
            } 
        }

        private ProvisioningType _provisioningType;

        public ProvisioningType ProvisioningType
        {
            get => _provisioningType;
            set 
            {
                Set(nameof(ProvisioningType), ref _provisioningType, value);

                if (value == ProvisioningType.Other)
                {
                    Validate(() => ValidationUtils.IsValidStringInput(Other), nameof(ProvisioningType));
                    GenerateReportCommand.RaiseCanExecuteChanged();
                }
                else
                {
                    if(GetErrors(nameof(ProvisioningType)) != null) RemoveError(nameof(ProvisioningType));
                }
            } 
        }

        protected override void OnIsBusyChanged()
        {
            GenerateReportCommand.RaiseCanExecuteChanged();
            IsEnabled = !IsBusy;
        }

        private string _other;

        public string Other
        {
            get => _other;
            set
            {
                var changed = Set(nameof(Other), ref _other, value);

                if (changed)
                {
                    Validate(() => ValidationUtils.IsValidStringInput(value), nameof(ProvisioningType));
                    GenerateReportCommand.RaiseCanExecuteChanged();
                }
            }
        }

        private bool _isEnabled;

        public bool IsEnabled
        {
            get => _isEnabled;
            private set => Set(nameof(IsEnabled), ref _isEnabled, value);
        }

        #endregion

        #region Comandi

        private RelayCommand _openReportWindowCommand;
        public RelayCommand OpenReportWindowCommand => _openReportWindowCommand
            ?? (_openReportWindowCommand = new RelayCommand(ExecuteOpenReportWindowCommand));

        private RelayCommand _generateReportCommand;
        public RelayCommand GenerateReportCommand => _generateReportCommand
            ?? (_generateReportCommand = new RelayCommand(ExecuteGenerateReportCommand, CanExecuteGenerateReportCommandAsync));

        #endregion

        private readonly IReportService _reportService;
        private readonly IWindowFactory _windowFactory;
        private readonly IMessenger _messenger;

        private UsageInfo[] _usageInfos;
        private InstallEntry[] _installEntries;
        private RecentFolderEntry[] _recentFolderEntries;
        private PrefetchInfoEntry[] _prefetchInfoEntries;
        private ShellBagEntry[] _shellBagEntries;
        private SessionEntry[] _sessionEntries;
        private SystemTimeChangedEntry[] _systemTimeChangedEntries;
        private UsbEntry[] _usbEntries;

        public ReportViewModel(IReportService reportService, IDialogService dialogService,
            IWindowFactory windowFactory, IMessenger messenger)
            : base(dialogService)
        {
            _reportService = reportService;
            _windowFactory = windowFactory;
            _messenger = messenger;

            IsBusy = false;

            _messenger.Register<OnUsageInfosChangedMessage>(this, HandleOnUsageInfosChangedMessage);
            _messenger.Register<OnInstallEntriesChangedMessage>(this, HandleOnInstallEntriesChangedMessage);
            _messenger.Register<OnRecentFolderEntriesChangedMessage>(this, HandleOnRecentFolderEntriesChangedMessage);
            _messenger.Register<OnPrefetchInfoEntriesChangedMessage>(this, HandleOnPrefetchInfoEntriesChangedMessage);
            _messenger.Register<OnShellBagEntriesChangedMessage>(this, HandleOnShellBagEntriesChangedMessage);
            _messenger.Register<OnSessionEntriesChangedMessage>(this, HandleOnSessionEntriesChangedMessage);
            _messenger.Register<OnSystemTimeChangedEntriesChangedMessage>(this, HandleOnSystemTimeChangedEntriesChangedMessage);
            _messenger.Register<OnUsbEntriesChangedMessage>(this, HandleOnUsbEntriesChangedMessage);
        }

        private void ExecuteOpenReportWindowCommand()
        {
            _windowFactory.OpenReportWindow();
        }

        private bool CanExecuteGenerateReportCommandAsync()
            => !HasErrors && !string.IsNullOrEmpty(InquirerName) && !string.IsNullOrEmpty(InquirerSurname) &&
                !string.IsNullOrEmpty(InquirerQualification) && !string.IsNullOrEmpty(ObjectDescription);

        private void ExecuteGenerateReportCommand()
            => Forget(GenerateReportAsync());

        private async Task GenerateReportAsync()
        {
            var destinationPath = Dialogs.SelectReportDestination();

            if (string.IsNullOrEmpty(destinationPath)) return;

            var token = BeginOperation();

            try
            {

                var content = new ReportContent(ProvisioningType, Other, InquirerSurname, InquirerName,
                    InquirerQualification, ObjectDescription, _usageInfos, _installEntries, _recentFolderEntries,
                    _prefetchInfoEntries, _shellBagEntries, _sessionEntries, _systemTimeChangedEntries, _usbEntries, destinationPath);
                
                var result = await _reportService.CreatePdfFileAsync(content, token);

                if (result.IsSuccess)
                {
                    Dialogs.ShowInfo(Activities_Inspector.Resources.ReportWindows_OperationComplete_Info);
                }
                else
                {
                    Dialogs.ShowError(result.Error);
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

        private void HandleOnUsageInfosChangedMessage(OnUsageInfosChangedMessage message)
        {
            _usageInfos = message.NewInfos.ToArray();
        }

        private void HandleOnInstallEntriesChangedMessage(OnInstallEntriesChangedMessage message)
        {
            _installEntries = message.NewInstallEntries.ToArray();
        }

        private void HandleOnRecentFolderEntriesChangedMessage(OnRecentFolderEntriesChangedMessage message)
        {
            _recentFolderEntries = message.NewRecentFoldersEntries.ToArray();
        }

        private void HandleOnPrefetchInfoEntriesChangedMessage(OnPrefetchInfoEntriesChangedMessage message)
        {
            _prefetchInfoEntries = message.NewPrefetchInfoEntries.ToArray();
        }

        private void HandleOnShellBagEntriesChangedMessage(OnShellBagEntriesChangedMessage message)
        {
            _shellBagEntries = message.NewShellBagEntries.ToArray();
        }

        private void HandleOnSessionEntriesChangedMessage(OnSessionEntriesChangedMessage message)
        {
            _sessionEntries = message.NewSessionEntries.ToArray();
        }

        private void HandleOnSystemTimeChangedEntriesChangedMessage(OnSystemTimeChangedEntriesChangedMessage message)
        {
            _systemTimeChangedEntries = message.NewTimeChangedEntries.ToArray();
        }

        private void HandleOnUsbEntriesChangedMessage(OnUsbEntriesChangedMessage message)
        {
            _usbEntries = message.NewUsbEntries.ToArray();
        }
    }
}