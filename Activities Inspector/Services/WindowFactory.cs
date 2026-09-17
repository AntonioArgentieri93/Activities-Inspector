using Activities_Inspector.Views;

namespace Activities_Inspector.Services
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
