using ProgettoInformaticaForense_Argentieri.Views;

namespace ProgettoInformaticaForense_Argentieri.Services
{
    public class WindowFactory : IWindowFactory
    {
        public void OpenReportWindow()
        {
            var loginWindow = new ReportWindow();
            loginWindow.ShowDialog();
        }
    }
}
