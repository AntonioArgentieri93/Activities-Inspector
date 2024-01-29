using System.Windows;
using System.Windows.Controls;

namespace ProgettoInformaticaForense_Argentieri.Views
{
    public partial class MainWindow : Window
    {
        public Frame Frame => FindName("MainFrame") as Frame;

        public MainWindow()
        {
            InitializeComponent();
        }
    }
}
