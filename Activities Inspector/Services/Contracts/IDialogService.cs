namespace Activities_Inspector.Services
{
    public interface IDialogService
    {
        void ShowError(string error);
        void ShowInfo(string message);
        string SelectReportDestination();
        string SelectFolder(string description);
    }
}
