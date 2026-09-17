using Activities_Inspector.Services;
using System.Threading.Tasks;
using Xunit;

namespace ActivitiesInspector.IntegrationTests.Services
{
    [Trait("Category", "Integration")]
    public class InstallEntriesBuilderTests
    {
        [Fact]
        public async Task GetInstallEntries_Returns_Success()
        {
            var service = new InstallEntriesBuilder();

            var result = await service.GetInstallEntriesAsync();

            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
        }
    }
}
