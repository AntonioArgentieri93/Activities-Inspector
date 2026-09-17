using Activities_Inspector.Services;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace ActivitiesInspector.IntegrationTests.Services
{
    [Trait("Category", "Integration")]
    public class UsageLogTimeServiceTests
    {
        [Fact]
        public async Task BuildUsageInfo_MachineNames_NeverEmpty()
        {
            var service = new UsageLogTimeService();

            var events = await service.GetSystemEventsAsync();

            Assert.True(events.IsSuccess);
            var infos = service.BuildUsageInfo(events.Value).ToList();
            Assert.All(infos, info => Assert.False(string.IsNullOrEmpty(info.MachineName)));
        }
    }
}
