using Activities_Inspector.Services;
using Activities_Inspector.Services.Evidence;
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
            var service = new RecentFilesService(new EvidenceSourceProvider());

            var result = await service.GetRecentFilesAsync();

            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
        }
    }
}
