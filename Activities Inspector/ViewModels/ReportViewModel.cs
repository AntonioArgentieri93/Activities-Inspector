using Activities_Inspector.Models;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using Activities_Inspector.Messages;
using Activities_Inspector.Models;
using Activities_Inspector.Services;
using Activities_Inspector.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Activities_Inspector.ViewModels
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
        private readonly IAuditTrail _auditTrail;

        private UsageInfo[] _usageInfos;
        private InstallEntry[] _installEntries;
        private RecentFolderEntry[] _recentFolderEntries;
        private PrefetchInfoEntry[] _prefetchInfoEntries;
        private ShellBagEntry[] _shellBagEntries;
        private bool _shellBagsPartial;
        private List<IntegrityRecord> _installManifest = new List<IntegrityRecord>();
        private List<IntegrityRecord> _recentManifest = new List<IntegrityRecord>();
        private List<IntegrityRecord> _prefetchManifest = new List<IntegrityRecord>();
        private List<IntegrityRecord> _usbManifest = new List<IntegrityRecord>();
        private SessionEntry[] _sessionEntries;
        private SystemTimeChangedEntry[] _systemTimeChangedEntries;
        private UsbEntry[] _usbEntries;

        public ReportViewModel(IReportService reportService, IDialogService dialogService,
            IWindowFactory windowFactory, IMessenger messenger, IAuditTrail auditTrail)
            : base(dialogService)
        {
            _reportService = reportService;
            _windowFactory = windowFactory;
            _messenger = messenger;
            _auditTrail = auditTrail;

            IsBusy = false;
            // Necessario: la base notifica OnIsBusyChanged solo su cambio
            // effettivo, quindi l'assegnazione qui sopra è no-op e IsEnabled
            // resterebbe false per sempre (finestra disabilitata).
            IsEnabled = true;

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
                    _prefetchInfoEntries, _shellBagEntries, _sessionEntries, _systemTimeChangedEntries, _usbEntries, destinationPath,
                    _shellBagsPartial, _installManifest.Concat(_recentManifest).Concat(_prefetchManifest).Concat(_usbManifest).ToArray(),
                    _auditTrail.Entries.ToArray());
                
                var result = await _reportService.CreatePdfFileAsync(content, token);

                if (result.IsSuccess)
                {
                    _auditTrail.Record(AuditCategory.Report, $"Report generato -> {destinationPath}");
                    Dialogs.ShowInfo(Activities_Inspector.Resources.ReportWindows_OperationComplete_Info);
                }
                else
                {
                    _auditTrail.Record(AuditCategory.Report, "Report fallito");
                    Dialogs.ShowError(result.Error);
                }
            }
            catch (OperationCanceledException)
            {
                _auditTrail.Record(AuditCategory.Report, "Report annullato");
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
            _installManifest = message.Manifest.ToList();
        }

        private void HandleOnRecentFolderEntriesChangedMessage(OnRecentFolderEntriesChangedMessage message)
        {
            _recentFolderEntries = message.NewRecentFoldersEntries.ToArray();
            _recentManifest = message.Manifest.ToList();
        }

        private void HandleOnPrefetchInfoEntriesChangedMessage(OnPrefetchInfoEntriesChangedMessage message)
        {
            _prefetchInfoEntries = message.NewPrefetchInfoEntries.ToArray();
            _prefetchManifest = message.Manifest.ToList();
        }

        private void HandleOnShellBagEntriesChangedMessage(OnShellBagEntriesChangedMessage message)
        {
            _shellBagEntries = message.NewShellBagEntries.ToArray();
            _shellBagsPartial = message.IsPartial;
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
            _usbManifest = message.Manifest.ToList();
        }
    }
}