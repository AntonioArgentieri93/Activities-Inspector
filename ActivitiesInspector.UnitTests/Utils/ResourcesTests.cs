using Xunit;

namespace ActivitiesInspector.UnitTests.Utils
{
    public class ResourcesTests
    {
        [Fact]
        public void SkippedItems_Tooltip_Is_Wired()
        {
            Assert.False(string.IsNullOrEmpty(
                Activities_Inspector.Resources.MainWindows_RecentFolder_SkippedItems_Tooltip));
        }
    }
}
