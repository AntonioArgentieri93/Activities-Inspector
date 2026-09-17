using Activities_Inspector.Services;
using System.Threading.Tasks;
using Xunit;

namespace ActivitiesInspector.IntegrationTests.Services
{
    [Trait("Category", "Integration")]
    public class RecentFilesServiceTests
    {
        [Fact]
        public async Task GetRecentFiles_Returns_Success()
        {
            var service = new RecentFilesService();

            var result = await service.GetRecentFilesAsync();

            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
        }
    }
}
