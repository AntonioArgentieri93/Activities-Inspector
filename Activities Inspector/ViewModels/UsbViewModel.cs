using Activities_Inspector.Messages;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using ProgettoInformaticaForense_Argentieri.Messages;
using ProgettoInformaticaForense_Argentieri.Models;
using ProgettoInformaticaForense_Argentieri.Pages;
using ProgettoInformaticaForense_Argentieri.Services;
using RawCopy;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management;
using System.Windows;

namespace ProgettoInformaticaForense_Argentieri.ViewModels
{
    public class UsbViewModel : ViewModelBase
    {
        #region Proprietà

        private ObservableCollection<UsbEntry> _usbEntries;

        public ObservableCollection<UsbEntry> UsbEntries
        {
            get => _usbEntries;
            set
            {
                var changed = Set(nameof(UsbEntries), ref _usbEntries, value);

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
                    LoadUsbEntriesCommand.RaiseCanExecuteChanged();
                    ExportCommand.RaiseCanExecuteChanged();
                }
            }
        }

        #endregion

        #region Comandi

        private RelayCommand _loadUsbEntriesCommand;
        public RelayCommand LoadUsbEntriesCommand => _loadUsbEntriesCommand
            ?? (_loadUsbEntriesCommand = new RelayCommand(ExecuteLoadUsbEntriesCommandAsync,
                CanExecuteLoadUsbEntriesCommand));

        private RelayCommand _exportCommand;
        public RelayCommand ExportCommand => _exportCommand
            ?? (_exportCommand = new RelayCommand(ExecuteExportCommandAsync,
                CanExecuteExportCommandAsync));

        #endregion

        private readonly IUsbTrackingService _usbTrackingService;
        private readonly IDialogService _dialogService;
        private readonly IEntriesExporter _entriesExporter;
        private readonly IMessenger _messenger;

        private ObservableCollection<UsbEntry> _temp;

        public UsbViewModel(IUsbTrackingService usbTrackingService, IDialogService dialogService,
            IEntriesExporter entriesExporter, IMessenger messenger)
        {
            _usbTrackingService = usbTrackingService;
            _dialogService = dialogService;
            _entriesExporter = entriesExporter;
            _messenger = messenger;

            UsbEntries = new ObservableCollection<UsbEntry>();
            _temp = new ObservableCollection<UsbEntry>();

            Listen();
            _messenger.Register<OnSortColumnMessage>(this, HandleOnSortColumnMessage);
        }

        private bool CanExecuteLoadUsbEntriesCommand()
            => IsBusy ? false : true;

        private async void ExecuteLoadUsbEntriesCommandAsync()
        {
            if (UsbEntries != null) UsbEntries.Clear();
            IsBusy = true;

            try
            {
                var isAdministrator = Helper.IsAdministrator();

                if(isAdministrator == false)
                {
                    _dialogService.ShowInfo("L'applicazione non è stata lanciata con privilegi di amministratore e pertanto " +
                        "le informazioni sugli orari di inserimento e rimozione del dispositivo non saranno disponibili.");
                }

                var result = await _usbTrackingService.BuildUsbEntries(isAdministrator); 

                if (result.IsSuccess)
                {
                    foreach(var usbEntry in result.Value)
                    {
                        SyncUsbEntries(new ObservableCollection<UsbEntry>(result.Value), _temp);
                    }

                    _messenger.Send(new OnUsbEntriesChangedMessage(result.Value));
                }
                else
                {
                    _dialogService.ShowError("Errore durante il caricamento degli elementi per la funzionalità richiesta.");
                }
            }
            catch (Exception ex)
            {
                _dialogService.ShowError(ex.Message + "\n" + ex.StackTrace);
            }

            IsBusy = false;
        }

        private bool CanExecuteExportCommandAsync()
            => IsBusy == false & UsbEntries != null;

        private async void ExecuteExportCommandAsync()
        {
            try
            {
                var exportResult = await _entriesExporter.SaveEntriesDataAsync(UsbEntries, EntryType.Usb);

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

        private void HandleOnSortColumnMessage(OnSortColumnMessage message)
        {
            if (UsbEntries == null || UsbEntries.Count == 0) return;

            var propertyType = (UsbPropertyType)message.NewPropertyType;
            var isAscending = message.NewIsAscending;

            if (isAscending)
            {
                switch (propertyType)
                {
                    case UsbPropertyType.DeviceName:
                        UsbEntries = new ObservableCollection<UsbEntry>(UsbEntries.OrderBy(d => d.DeviceName));
                        break;
                    case UsbPropertyType.SerialNumber:
                        UsbEntries = new ObservableCollection<UsbEntry>(UsbEntries.OrderBy(d => d.SerialNumber));
                        break;
                    case UsbPropertyType.VendorId:
                        UsbEntries = new ObservableCollection<UsbEntry>(UsbEntries.OrderBy(d => d.VendorId));
                        break;
                    case UsbPropertyType.ProductId:
                        UsbEntries = new ObservableCollection<UsbEntry>(UsbEntries.OrderBy(d => d.ProductId));
                        break;
                    case UsbPropertyType.UsbClass:
                        UsbEntries = new ObservableCollection<UsbEntry>(UsbEntries.OrderBy(d => d.UsbClass));
                        break;
                    case UsbPropertyType.LastConnected:
                        UsbEntries = new ObservableCollection<UsbEntry>(UsbEntries.OrderBy(d => d.LastConnected));
                        break;
                    case UsbPropertyType.LastRemoved:
                        UsbEntries = new ObservableCollection<UsbEntry>(UsbEntries.OrderBy(d => d.LastRemoved));
                        break;
                }
            }
            else
            {
                switch (propertyType)
                {
                    case UsbPropertyType.DeviceName:
                        UsbEntries = new ObservableCollection<UsbEntry>(UsbEntries.OrderByDescending(d => d.DeviceName));
                        break;
                    case UsbPropertyType.SerialNumber:
                        UsbEntries = new ObservableCollection<UsbEntry>(UsbEntries.OrderByDescending(d => d.SerialNumber));
                        break;
                    case UsbPropertyType.VendorId:
                        UsbEntries = new ObservableCollection<UsbEntry>(UsbEntries.OrderByDescending(d => d.VendorId));
                        break;
                    case UsbPropertyType.ProductId:
                        UsbEntries = new ObservableCollection<UsbEntry>(UsbEntries.OrderByDescending(d => d.ProductId));
                        break;
                    case UsbPropertyType.UsbClass:
                        UsbEntries = new ObservableCollection<UsbEntry>(UsbEntries.OrderByDescending(d => d.UsbClass));
                        break;
                    case UsbPropertyType.LastConnected:
                        UsbEntries = new ObservableCollection<UsbEntry>(UsbEntries.OrderByDescending(d => d.LastConnected));
                        break;
                    case UsbPropertyType.LastRemoved:
                        UsbEntries = new ObservableCollection<UsbEntry>(UsbEntries.OrderByDescending(d => d.LastRemoved));
                        break;
                }
            }

            _messenger.Send(new OnUsbEntriesChangedMessage(UsbEntries.ToList()));
        }

        #region Registrazione eventi

        private delegate void UpdateDelegate();

        private void Listen()
        {
            var insertQuery = new WqlEventQuery("SELECT * FROM __InstanceCreationEvent WITHIN 2 WHERE TargetInstance ISA 'Win32_PnPEntity'");
            ManagementEventWatcher insertWatcher = new ManagementEventWatcher(insertQuery);
            insertWatcher.EventArrived += new EventArrivedEventHandler(OnDeviceInserted);
            insertWatcher.Start();

            var removeQuery = new WqlEventQuery("SELECT * FROM __InstanceDeletionEvent WITHIN 2 WHERE TargetInstance ISA 'Win32_PnPEntity'");
            ManagementEventWatcher removeWatcher = new ManagementEventWatcher(removeQuery);
            removeWatcher.EventArrived += new EventArrivedEventHandler(OnDeviceRemoved);
            removeWatcher.Start();
        }

        private void OnDeviceInserted(object sender, EventArrivedEventArgs e)
        {
            EvaluateState(e, true);
        }

        private void OnDeviceRemoved(object sender, EventArrivedEventArgs e)
        {
            EvaluateState(e, false);
        }

        private void EvaluateState(EventArrivedEventArgs e, bool newIsPlugged)
        {
            ManagementBaseObject instance = (ManagementBaseObject)e.NewEvent["TargetInstance"];

            var deviceIdValue = instance.Properties["DeviceID"];

            if (deviceIdValue == null) return;

            var value = deviceIdValue.Value;

            if (value == null) return;

            var strValue = value.ToString();

            if (strValue.Contains("VID") && strValue.Contains("PID"))
            {
                var splitResult = strValue.Split("\\");
                
                if (splitResult.Length != 3) return;

                var desiredValue = splitResult[1];

                var intermediateResult = desiredValue.Split('&');

                var vid = intermediateResult[0].Replace("VID_", string.Empty);
                var pid = intermediateResult[1].Replace("PID_", string.Empty);

                var device = UsbEntries?.Where(ue => ue.VendorId == vid && ue.ProductId == pid).FirstOrDefault() ?? null;
                
                if(device != null)
                {
                    device.Plugged = newIsPlugged;

                    var now = DateTimeOffset.UtcNow.ToLocalTime();

                    if (newIsPlugged)
                    {
                        device.LastConnected = now;
                    }
                    else
                    {
                        device.LastRemoved = now;
                    }

                    Application.Current.Dispatcher.Invoke(new UpdateDelegate(UpdateUsbEntries));
                }
                else if(device == null && newIsPlugged)
                {
                    var plugged = true;

                    var serialNumber = splitResult[2];

                    var deviceName = (string)instance.Properties["Caption"].Value;

                    var usbClass = (string)instance.Properties["PNPClass"].Value;

                    var lastConnected = DateTimeOffset.Now.ToLocalTime();

                    var newEntry = new UsbEntry(plugged, deviceName, serialNumber, vid, pid, usbClass, lastConnected, null);

                    if(_temp.Any(ue => ue.SerialNumber == newEntry.SerialNumber && ue.VendorId == newEntry.VendorId &&
                        ue.ProductId == newEntry.ProductId) == false)
                    {
                        _temp.Add(newEntry);
                    }
                }
            }
        }

        private void UpdateUsbEntries()
        {
            var list = UsbEntries.ToList();
            UsbEntries = new ObservableCollection<UsbEntry>(list);
        }

        private void SyncUsbEntries(ObservableCollection<UsbEntry> scanResult, ObservableCollection<UsbEntry> tempResult)
        {
            UsbEntries.Clear();

            foreach (var usbEntry in scanResult)
            {
                UsbEntries.Add(usbEntry);
            }

            foreach (var usbEntry in tempResult)
            {
                if (UsbEntries.Any(ue => ue.SerialNumber == usbEntry.SerialNumber && ue.VendorId == usbEntry.VendorId &&
                         ue.ProductId == usbEntry.ProductId) == false)
                {
                    UsbEntries.Add(usbEntry);
                }
            }
        }

        #endregion
    }
}
