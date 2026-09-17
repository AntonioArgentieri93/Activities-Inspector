using Activities_Inspector.Services;
using System.Collections.Generic;

namespace ActivitiesInspector.UnitTests.Doubles
{
    public sealed class TestDialogService : IDialogService
    {
        public readonly List<string> Errors = new List<string>();
        public readonly List<string> Infos = new List<string>();

        public string DestinationToSelect { get; set; }

        public void ShowError(string error)
        {
            Errors.Add(error);
        }

        public void ShowInfo(string message)
        {
            Infos.Add(message);
        }

        public string SelectReportDestination()
        {
            return DestinationToSelect;
        }
    }
}
