using Activities_Inspector.Services;

namespace ActivitiesInspector.UnitTests.Doubles
{
    public sealed class FakeWindowFactory : IWindowFactory
    {
        public int OpenCalls { get; private set; }

        public void OpenReportWindow()
        {
            OpenCalls++;
        }
    }
}
