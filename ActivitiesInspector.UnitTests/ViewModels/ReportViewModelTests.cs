using ActivitiesInspector.UnitTests.Doubles;
using Activities_Inspector.Services.Evidence;
using Activities_Inspector.ViewModels;
using GalaSoft.MvvmLight.Messaging;
using Xunit;

namespace ActivitiesInspector.UnitTests.ViewModels
{
    public class ReportViewModelTests
    {
        private static ReportViewModel Create()
        {
            return new ReportViewModel(
                new FakeReportService(),
                new TestDialogService(),
                new FakeWindowFactory(),
                Messenger.Default,
                new FakeAuditTrail(),
                new EvidenceSourceProvider());
        }

        [Fact]
        public void Initial_State_Is_Enabled_And_Idle()
        {
            var vm = Create();

            Assert.False(vm.IsBusy);
            Assert.True(vm.IsEnabled);
        }

        [Fact]
        public void Generate_Without_Destination_Does_Nothing()
        {
            var dialogs = new TestDialogService();
            var vm = new ReportViewModel(
                new FakeReportService(),
                dialogs,
                new FakeWindowFactory(),
                Messenger.Default,
                new FakeAuditTrail(),
                new EvidenceSourceProvider());

            vm.GenerateReportCommand.Execute(null);

            Assert.False(vm.IsBusy);
            Assert.True(vm.IsEnabled);
            Assert.Empty(dialogs.Errors);
        }
    }
}
