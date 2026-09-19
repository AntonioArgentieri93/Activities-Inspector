using Activities_Inspector.Messages;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using Activities_Inspector.Messages;
using Activities_Inspector.Models;
using Activities_Inspector.Pages;
using Activities_Inspector.Services;
using CSharpFunctionalExtensions;
using RawCopy;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Activities_Inspector.ViewModels
{
    public class UsbViewModel : FeatureViewModelBase<UsbEntry>
    {
        #region Proprietà

        public ObservableCollection<UsbEntry> UsbEntries
        {
            get => Entries;
            set
            {
                Entries = value;
                RaisePropertyChanged(nameof(UsbEntries));
            }
        }

        protected override void OnIsBusyChanged()
        {
            LoadUsbEntriesCommand.RaiseCanExecuteChanged();
        }

        #endregion

        #region Comandi

        private RelayCommand _loadUsbEntriesCommand;
        public RelayCommand LoadUsbEntriesCommand => _loadUsbEntriesCommand
            ?? (_loadUsbEntriesCommand = new RelayCommand(ExecuteLoadUsbEntriesCommand,
                CanExecuteLoadUsbEntriesCommand));

        #endregion

        private readonly IUsbTrackingService _usbTrackingService;
        private readonly IMessenger _messenger;
        private ObservableCollection<UsbEntry> _temp;

        public UsbViewModel(IUsbTrackingService usbTrackingService, IDialogService dialogService,
            IEntriesExporter entriesExporter, IMessenger messenger, IAuditTrail auditTrail, Services.Evidence.IEvidenceSourceProvider sources)
            : base(dialogService, entriesExporter, auditTrail, sources)
        {
            _usbTrackingService = usbTrackingService;
            _messenger = messenger;

            UsbEntries = new ObservableCollection<UsbEntry>();
            _temp = new ObservableCollection<UsbEntry>();

            Listen();
            _messenger.Register<OnSortColumnMessage>(this, HandleOnSortColumnMessage);
        }

        private bool CanExecuteLoadUsbEntriesCommand()
            => !IsBusy;

        private void ExecuteLoadUsbEntriesCommand()
            => Forget(LoadAsync());

        protected override EntryType EntryType => EntryType.Usb;

        protected override bool CheckAccess()
        {
            if (!Helper.IsAdministrator())
            {
                Dialogs.ShowInfo("L'applicazione non è stata lanciata con privilegi di amministratore e pertanto " +
                    "le informazioni sugli orari di inserimento e rimozione del dispositivo non saranno disponibili.");
            }

            return true;
        }

        protected override async Task<Result<List<UsbEntry>>> LoadEntriesAsync(CancellationToken token)
        {
            return await _usbTrackingService.BuildUsbEntriesAsync(Helper.IsAdministrator(), token);
        }

        protected override void SetEntries(ObservableCollection<UsbEntry> entries)
        {
            UsbEntries = entries;
        }

        protected override void PublishEntries(List<UsbEntry> entries)
        {
            // SyncUsbEntries è idempotente: una singola chiamata equivale
            // al ciclo originale che la invocava N volte con gli stessi dati.
            SyncUsbEntries(new ObservableCollection<UsbEntry>(entries), _temp);
            _messenger.Send(new OnUsbEntriesChangedMessage(entries, _usbTrackingService.LastIntegrityManifest));
        }

        private void HandleOnSortColumnMessage(OnSortColumnMessage message)
        {
            ApplySort(message.NewPropertyType, message.NewIsAscending);
        }

        #region Registrazione eventi

        private delegate void UpdateDelegate();

        private void Listen()
        {
            var insertQuery = new WqlEventQuery("SELECT * FROM __InstanceCreationEvent WITHIN 2 WHERE TargetInstance ISA 'Win32_PnPEntity'");
            ManagementEventWatcher insertWatcher = new ManagementEventWatcher(insertQuery);
            insertWatcher.EventArrived += new EventArrivedEventHandler(OnDevicePlugged);
            insertWatcher.Start();

            var removeQuery = new WqlEventQuery("SELECT * FROM __InstanceDeletionEvent WITHIN 2 WHERE TargetInstance ISA 'Win32_PnPEntity'");
            ManagementEventWatcher removeWatcher = new ManagementEventWatcher(removeQuery);
            removeWatcher.EventArrived += new EventArrivedEventHandler(OnDeviceUnplagged);
            removeWatcher.Start();
        }

        private void OnDevicePlugged(object sender, EventArrivedEventArgs e)
        {
            EvaluateState(e, true);
        }

        private void OnDeviceUnplagged(object sender, EventArrivedEventArgs e)
        {
            EvaluateState(e, false);
        }

        private void EvaluateState(EventArrivedEventArgs e, bool newIsPlugged)
        {
            // Su sorgente offline gli eventi WMI riguarderebbero il PC del
            // perito, non l'immagine: mai applicarli ai risultati.
            if (!Sources.Current.IsLive) return;

            var instance = (ManagementBaseObject)e.NewEvent["TargetInstance"];

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

                if (device != null)
                {
                    device.Plugged = newIsPlugged;

                    var now = DateTimeOffset.Now;

                    if (newIsPlugged)
                    {
                        device.LastConnected = now;
                        device.LastRemoved = null;
                    }
                    else
                    {
                        device.LastRemoved = now;
                    }

                    Application.Current.Dispatcher.Invoke(new UpdateDelegate(UpdateUsbEntries));
                }
                else if (device == null && newIsPlugged)
                {
                    var plugged = true;

                    var serialNumber = splitResult[2];
                    var deviceName = (string)instance.Properties["Caption"].Value;
                    var usbClass = (string)instance.Properties["PNPClass"].Value;
                    var lastConnected = DateTimeOffset.Now;
                    var newEntry = new UsbEntry(plugged, deviceName, serialNumber,
                        vid, pid, usbClass, lastConnected, null);

                    if (_temp.Any(ue => ue.SerialNumber == newEntry.SerialNumber && ue.VendorId == newEntry.VendorId &&
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