using Activities_Inspector.Services;
using Xunit;

namespace ActivitiesInspector.UnitTests.Services
{
    public class RecentFilesPathTests
    {
        [Fact]
        public void Local_Path_Wins()
        {
            Assert.Equal(
                @"C:\Docs\file.txt",
                RecentFilesService.ResolveTargetPath(@"C:\Docs\file.txt", @"\\srv\share", @"\dir\file.txt"));
        }

        [Fact]
        public void Unc_Share_And_CommonPath_Combine()
        {
            Assert.Equal(
                @"\\srv\share\dir\file.txt",
                RecentFilesService.ResolveTargetPath(null, @"\\srv\share", @"\dir\file.txt"));
        }

        [Fact]
        public void Share_Without_Suffix_Returns_Share()
        {
            Assert.Equal(
                @"\\srv\share",
                RecentFilesService.ResolveTargetPath(string.Empty, @"\\srv\share\", string.Empty));
        }

        [Fact]
        public void All_Empty_Returns_Empty()
        {
            Assert.Equal(
                string.Empty,
                RecentFilesService.ResolveTargetPath(null, null, null));
        }
    }
}
