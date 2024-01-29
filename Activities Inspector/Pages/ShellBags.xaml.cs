using Activities_Inspector.Messages;
using GalaSoft.MvvmLight.Messaging;
using System;
using System.Windows.Controls;

namespace ProgettoInformaticaForense_Argentieri.Pages
{
    public partial class ShellBags : Page
    {
        private int _clickCount;
        private string _lastCellName;

        private readonly IMessenger _messenger;

        public ShellBags()
        {
            InitializeComponent();

            _messenger = Messenger.Default;
        }

        private void TextBlock_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var control = (TextBlock)sender;
            var cellName = control.Text;

            ShellBagsPropertyType propertyType;

            switch (cellName)
            {
                case string str when str.Equals(Activities_Inspector.Resources.MainWindows_ShellBags_AbsolutePath):
                    propertyType = ShellBagsPropertyType.AbsolutePath;
                    break;
                case string str when str.Equals(Activities_Inspector.Resources.MainWindows_ShellBags_LastRegistryWriteDate):
                    propertyType = ShellBagsPropertyType.LastRegistryWriteDate;
                    break;
                case string str when str.Equals(Activities_Inspector.Resources.MainWindows_ShellBags_RegistryPath):
                    propertyType = ShellBagsPropertyType.RegistryPath;
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

    public enum ShellBagsPropertyType
    {
        AbsolutePath,
        LastRegistryWriteDate,
        RegistryPath
    }
}
