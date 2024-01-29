using Activities_Inspector.Messages;
using GalaSoft.MvvmLight.Messaging;
using System;
using System.Windows.Controls;

namespace ProgettoInformaticaForense_Argentieri.Pages
{
    public partial class InstalledPrograms : Page
    {
        private int _clickCount;
        private string _lastCellName;

        private readonly IMessenger _messenger;

        public InstalledPrograms()
        {
            InitializeComponent();

            _messenger = Messenger.Default;
        }

        private void TextBlock_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var control = (TextBlock)sender;
            var cellName = control.Text;

            InstalledProgramsPropertyType propertyType;

            switch (cellName)
            {
                case string str when str.Equals( Activities_Inspector.Resources.MainWindows_InstallEntry_FileName):
                    propertyType = InstalledProgramsPropertyType.FileName;
                    break;
                case string str when str.Equals(Activities_Inspector.Resources.MainWindows_InstallEntry_DataSource):
                    propertyType = InstalledProgramsPropertyType.DataSource;
                    break;
                case string str when str.Equals(Activities_Inspector.Resources.MainWindows_InstallEntry_FullPath):
                    propertyType = InstalledProgramsPropertyType.FullPath;
                    break;
                case string str when str.Equals(Activities_Inspector.Resources.MainWindows_InstallEntry_installDate):
                    propertyType = InstalledProgramsPropertyType.InstallDate;
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

    public enum InstalledProgramsPropertyType
    {
        FileName,
        DataSource,
        FullPath,
        InstallDate
    }
}
