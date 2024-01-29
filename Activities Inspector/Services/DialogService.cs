using Activities_Inspector;
using Microsoft.WindowsAPICodePack.Dialogs;
using ProgettoInformaticaForense_Argentieri.Views;
using System;
using System.Linq;
using System.Windows;

namespace ProgettoInformaticaForense_Argentieri.Services
{
    public class DialogService : IDialogService
    {
        private readonly string _caption = Resources.AppName;

        public void ShowError(string error)
        {
            MessageBoxButton button = MessageBoxButton.OK;
            MessageBoxImage icon = MessageBoxImage.Error;

            MessageBox.Show(error, _caption, button, icon);
        }

        public void ShowInfo(string message)
        {
            MessageBoxButton button = MessageBoxButton.OK;
            MessageBoxImage icon = MessageBoxImage.Information;

            MessageBox.Show(message, _caption, button, icon);
        }

        public string SelectReportDestination()
        {
            var window = Application.Current.Windows.OfType<ReportWindow>().SingleOrDefault(w => w.IsActive);
            var dialog = new CommonOpenFileDialog();

            dialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            dialog.IsFolderPicker = true;

            return dialog.ShowDialog(window) == CommonFileDialogResult.Ok ? dialog.FileName : null;
        }
    }
}
