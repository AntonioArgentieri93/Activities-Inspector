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

        [Theory]
        [InlineData(@"C:\Users\", @"anton\Downloads\x.png", @"C:\Users\anton\Downloads\x.png")]
        [InlineData(@"C:\Users\anton\", @"Downloads\x.png", @"C:\Users\anton\Downloads\x.png")]
        [InlineData(@"C:\", @"\Data\x.png", @"C:\Data\x.png")]
        public void Local_Base_Path_Of_Shared_Folder_Combines_With_Suffix(string localBase, string suffix, string expected)
        {
            // File in cartella condivisa (C:\Users condivisa come \\MSI\Users): Windows e NirSoft mostrano il percorso locale
            Assert.Equal(expected, RecentFilesService.ResolveTargetPath(localBase, @"\\MSI\Users", suffix));
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
