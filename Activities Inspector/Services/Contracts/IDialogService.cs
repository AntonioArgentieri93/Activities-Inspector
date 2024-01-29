namespace ProgettoInformaticaForense_Argentieri.Services
{
    public interface IDialogService
    {
        void ShowError(string error);
        void ShowInfo(string message);
        string SelectReportDestination();
    }
}
