using Activities_Inspector.Messages;
using GalaSoft.MvvmLight.Messaging;
using System;
using System.Windows.Controls;

namespace ProgettoInformaticaForense_Argentieri.Pages
{
    public partial class Recents : Page
    {
        private int _clickCount;
        private string _lastCellName;

        private readonly IMessenger _messenger;

        public Recents()
        {
            InitializeComponent();

            _messenger = Messenger.Default;
        }

        private void TextBlock_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var control = (TextBlock)sender;
            var cellName = control.Text;

            RecentsFolderEntryPropertyType propertyType;

            switch (cellName)
            {
                case string str when str.Equals(Activities_Inspector.Resources.MainWindows_RecentFolder_FileName):
                    propertyType = RecentsFolderEntryPropertyType.FileName;
                    break;
                case string str when str.Equals(Activities_Inspector.Resources.MainWindows_RecentFolder_DataSource):
                    propertyType = RecentsFolderEntryPropertyType.DataSource;
                    break;
                case string str when str.Equals(Activities_Inspector.Resources.MainWindows_RecentFolder_FullPath):
                    propertyType = RecentsFolderEntryPropertyType.FullPath;
                    break;
                case string str when str.Equals(Activities_Inspector.Resources.MainWindows_RecentFolder_ActionTime):
                    propertyType = RecentsFolderEntryPropertyType.ActionTime;
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

    public enum RecentsFolderEntryPropertyType
    {
        ActionTime,
        FileName,
        DataSource,
        FullPath
    }
}
