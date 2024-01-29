using Activities_Inspector.Messages;
using GalaSoft.MvvmLight.Messaging;
using System;
using System.Windows.Controls;

namespace ProgettoInformaticaForense_Argentieri.Pages
{
    public partial class Usb : Page
    {
        private int _clickCount;
        private string _lastCellName;

        private readonly IMessenger _messenger;

        public Usb()
        {
            InitializeComponent();

            _messenger = Messenger.Default;
        }

        private void TextBlock_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var control = (TextBlock)sender;
            var cellName = control.Text;

            UsbPropertyType propertyType;

            switch (cellName)
            {
                case string str when str.Equals(Activities_Inspector.Resources.MainWindow_Usb_DeviceName):
                    propertyType = UsbPropertyType.DeviceName;
                    break;
                case string str when str.Equals(Activities_Inspector.Resources.MainWindow_Usb_SerialNumber):
                    propertyType = UsbPropertyType.SerialNumber;
                    break;
                case string str when str.Equals(Activities_Inspector.Resources.MainWindow_Usb_VendorId):
                    propertyType = UsbPropertyType.VendorId;
                    break;
                case string str when str.Equals(Activities_Inspector.Resources.MainWindow_Usb_ProductId):
                    propertyType = UsbPropertyType.ProductId;
                    break;
                case string str when str.Equals(Activities_Inspector.Resources.MainWindow_Usb_UsbClass):
                    propertyType = UsbPropertyType.UsbClass;
                    break;
                case string str when str.Equals(Activities_Inspector.Resources.MainWindow_Usb_PluggedTime):
                    propertyType = UsbPropertyType.LastConnected;
                    break;
                case string str when str.Equals(Activities_Inspector.Resources.MainWindow_Usb_UnpluggedTime):
                    propertyType = UsbPropertyType.LastRemoved;
                    break;
                default:
                    throw new ArgumentException("Il valore di 'cellName' non corrisponde a nessuna delle proprietà della classe.");
            }

            if (_lastCellName == cellName)
            {
                _clickCount++;
            }
            else
            {
                _lastCellName = cellName;
                _clickCount = 1;
            }

            if (_clickCount % 2 == 0)
            {
                _messenger.Send(new OnSortColumnMessage(propertyType, false));
            }
            else
            {
                _messenger.Send(new OnSortColumnMessage(propertyType, true));
            }
        }
    }

    public enum UsbPropertyType
    {
        DeviceName,
        SerialNumber,
        VendorId,
        ProductId,
        UsbClass,
        LastConnected,
        LastRemoved
    }
}
