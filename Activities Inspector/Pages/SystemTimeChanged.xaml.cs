using Activities_Inspector.Messages;
using GalaSoft.MvvmLight.Messaging;
using System;
using System.Windows.Controls;

namespace Activities_Inspector.Pages
{
    public partial class SystemTimeChanged : Page
    {
        private int _clickCount;
        private string _lastCellName;

        private readonly IMessenger _messenger;

        public SystemTimeChanged()
        {
            InitializeComponent();

            _messenger = Messenger.Default;
        }

        private void TextBlock_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var control = (TextBlock)sender;
            var cellName = control.Text;

            SystemTimeChangedPropertyType propertyType;

            switch (cellName)
            {
                case string str when str.Equals(Activities_Inspector.Resources.SystemTimeChanged_AccountName_ColumnHeader):
                    propertyType = SystemTimeChangedPropertyType.AccountName;
                    break;
                case string str when str.Equals(Activities_Inspector.Resources.SystemTimeChanged_TimeGenerated_ColumnHeader):
                    propertyType = SystemTimeChangedPropertyType.TimeGenerated;
                    break;
                case string str when str.Equals(Activities_Inspector.Resources.SystemTimeChanged_OldTime_ColumnHeader):
                    propertyType = SystemTimeChangedPropertyType.OldTime;
                    break;
                case string str when str.Equals(Activities_Inspector.Resources.SystemTimeChanged_NewTime_ColumnHeader):
                    propertyType = SystemTimeChangedPropertyType.NewTime;
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

    public enum SystemTimeChangedPropertyType
    {
        AccountName,
        TimeGenerated,
        OldTime,
        NewTime
    }
}
