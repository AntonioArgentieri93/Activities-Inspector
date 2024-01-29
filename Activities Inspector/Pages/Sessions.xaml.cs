using Activities_Inspector.Messages;
using GalaSoft.MvvmLight.Messaging;
using System;
using System.Windows.Controls;

namespace ProgettoInformaticaForense_Argentieri.Pages
{
    public partial class Sessions : Page
    {
        private int _clickCount;
        private string _lastCellName;

        private readonly IMessenger _messenger;

        public Sessions()
        {
            InitializeComponent();

            _messenger = Messenger.Default;
        }

        private void TextBlock_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var control = (TextBlock)sender;
            var cellName = control.Text;

            SessionPropertyType propertyType;

            switch (cellName)
            {
                case string str when str.Equals(Activities_Inspector.Resources.MainWindows_Sessions_UserName):
                    propertyType = SessionPropertyType.UserName;
                    break;
                case string str when str.Equals(Activities_Inspector.Resources.MainWindows_Sessions_Group):
                    propertyType = SessionPropertyType.Group;
                    break;
                case string str when str.Equals(Activities_Inspector.Resources.MainWindows_Sessions_MachineName):
                    propertyType = SessionPropertyType.MachineName;
                    break;
                case string str when str.Equals(Activities_Inspector.Resources.MainWindows_Sessions_LogOnTime):
                    propertyType = SessionPropertyType.LogOnTime;
                    break;
                case string str when str.Equals(Activities_Inspector.Resources.MainWindows_Sessions_LogOffTime):
                    propertyType = SessionPropertyType.LogOffTime;
                    break;
                case string str when str.Equals(Activities_Inspector.Resources.MainWindows_Sessions_Duration):
                    propertyType = SessionPropertyType.Duration;
                    break;
                case string str when str.Equals(Activities_Inspector.Resources.MainWindows_Sessions_NetworkAddress):
                    propertyType = SessionPropertyType.NetworkAddress;
                    break;
                case string str when str.Equals(Activities_Inspector.Resources.MainWindows_Sessions_AccessType):
                    propertyType = SessionPropertyType.AccessType;
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

    public enum SessionPropertyType
    {
        UserName, 
        Group, 
        MachineName,
        LogOnTime,
        LogOffTime,
        Duration,
        NetworkAddress,
        AccessType
    }
}
