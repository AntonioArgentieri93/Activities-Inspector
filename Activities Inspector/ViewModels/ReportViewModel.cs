using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using ProgettoInformaticaForense_Argentieri.Messages;
using ProgettoInformaticaForense_Argentieri.Models;
using ProgettoInformaticaForense_Argentieri.Services;
using ProgettoInformaticaForense_Argentieri.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ProgettoInformaticaForense_Argentieri.ViewModels
{
    public class ReportViewModel : ValidationViewModelBase
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

        private bool _isBusy;

        public bool IsBusy
        {
            get => _isBusy;
            set 
            {
                Set(nameof(IsBusy), ref _isBusy, value);
                IsEnabled = !value;
            } 
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
            ?? (_generateReportCommand = new RelayCommand(ExecuteGenerateReportCommandAsync, CanExecuteGenerateReportCommandAsync));

        #endregion

        private readonly INetService _netService;
        private readonly IReportService _reportService;
        private readonly IDialogService _dialogService;
        private readonly IWindowFactory _windowFactory;
        private readonly IMessenger _messenger;

        private string _machineName;
        private List<string> _privateIpAddresses;
        private string _publicIpAddress;

        private UsageInfo[] _usageInfos;
        private InstallEntry[] _installEntries;
        private RecentFolderEntry[] _recentFolderEntries;
        private PrefetchInfoEntry[] _prefetchInfoEntries;
        private ShellBagEntry[] _shellBagEntries;
        private SessionEntry[] _sessionEntries;
        private SystemTimeChangedEntry[] _systemTimeChangedEntries;
        private UsbEntry[] _usbEntries;

        public ReportViewModel(INetService netService, IReportService reportService, IDialogService dialogService,
            IWindowFactory windowFactory, IMessenger messenger)
        {
            _netService = netService;
            _reportService = reportService;
            _dialogService = dialogService;
            _windowFactory = windowFactory;
            _messenger = messenger;

            _machineName = System.Net.Dns.GetHostName();
            _privateIpAddresses = _netService.GetAvailablePrivateIPs().ToList();
            _publicIpAddress = _netService.GetPublicIPAddress();

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
            => !HasErrors && (string.IsNullOrEmpty(InquirerName) == false && string.IsNullOrEmpty(InquirerSurname) == false &&
                string.IsNullOrEmpty(InquirerQualification) == false && string.IsNullOrEmpty(ObjectDescription) == false);

        private async void ExecuteGenerateReportCommandAsync()
        {
            try
            {
                var destinationPath = _dialogService.SelectReportDestination();

                if (string.IsNullOrEmpty(destinationPath)) return;

                IsBusy = true;

                var result = await _reportService.CreatePdfFile(ProvisioningType, Other, InquirerSurname, InquirerName, 
                    InquirerQualification, ObjectDescription, _usageInfos, _installEntries, _recentFolderEntries, 
                    _prefetchInfoEntries, _shellBagEntries, _sessionEntries, _systemTimeChangedEntries, _usbEntries, destinationPath);

                if (result.IsSuccess)
                {
                    _dialogService.ShowInfo(Activities_Inspector.Resources.ReportWindows_OperationComplete_Info);
                }
            }
            catch (Exception ex)
            {
                _dialogService.ShowError(ex.Message);
            }

            IsBusy = false;
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
