using System.Windows;
using System.Windows.Controls;

namespace Activities_Inspector.Views
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
