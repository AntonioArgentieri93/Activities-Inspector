using Activities_Inspector.Models;
using Activities_Inspector.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace ActivitiesInspector.UnitTests.Services
{
    public class InstallEntriesFilterTests
    {
        [Fact]
        public void Normal_App_Is_Included()
        {
            Assert.True(InstallEntriesBuilder.ShouldInclude("7-Zip", null, null, null));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Missing_Name_Is_Excluded(string displayName)
        {
            Assert.False(InstallEntriesBuilder.ShouldInclude(displayName, null, null, null));
        }

        [Theory]
        [InlineData("1", false)]
        [InlineData("0", true)]
        [InlineData(null, true)]
        public void SystemComponent_Filters(string systemComponent, bool expected)
        {
            Assert.Equal(expected, InstallEntriesBuilder.ShouldInclude("App", systemComponent, null, null));
        }

        [Fact]
        public void Child_Entry_Is_Excluded()
        {
            Assert.False(InstallEntriesBuilder.ShouldInclude("Componente", null, "{parent-guid}", null));
        }

        [Theory]
        [InlineData("Hotfix", false)]
        [InlineData("Security Update", false)]
        [InlineData("Update", false)]
        [InlineData("security update", false)]
        [InlineData(null, true)]
        [InlineData("", true)]
        [InlineData("Service Pack", true)]
        public void Update_Releases_Filtered(string releaseType, bool expected)
        {
            Assert.Equal(expected, InstallEntriesBuilder.ShouldInclude("KB123456", null, null, releaseType));
        }

        [Fact]
        public void Dedupe_Enriches_Path_With_Event_Date()
        {
            var entries = new List<InstallEntry>
            {
                new InstallEntry("App", "HKLM", "C:\\App", null),
                new InstallEntry("APP", string.Empty, string.Empty, new DateTime(2024, 1, 15))
            };

            var result = InstallEntriesBuilder.DedupeEntries(entries);

            var single = Assert.Single(result);
            Assert.Equal("HKLM", single.DataSource);
            Assert.Equal("C:\\App", single.FullPath);
            Assert.Equal(new DateTime(2024, 1, 15), single.InstallDate);
        }

        [Fact]
        public void Dedupe_Keeps_Distinct_And_Stealth_Entries()
        {
            var entries = new List<InstallEntry>
            {
                new InstallEntry("App", "HKLM", "C:\\App", null),
                new InstallEntry("Altro", "HKCU", "C:\\Altro", null),
                new InstallEntry("StealthTool", string.Empty, string.Empty, new DateTime(2024, 3, 1))
            };

            var result = InstallEntriesBuilder.DedupeEntries(entries);

            Assert.Equal(3, result.Count);
            Assert.Contains(result, e => e.FileName == "StealthTool");
        }

        [Fact]
        public void Dedupe_Drops_Empty_Names()
        {
            var entries = new List<InstallEntry>
            {
                new InstallEntry(string.Empty, "HKLM", "C:\\App", null),
                new InstallEntry("App", "HKLM", "C:\\App", null)
            };

            var result = InstallEntriesBuilder.DedupeEntries(entries);

            Assert.Equal("App", Assert.Single(result).FileName);
        }

        [Fact]
        public void Dedupe_Null_Returns_Empty()
        {
            Assert.Empty(InstallEntriesBuilder.DedupeEntries(null));
        }
    }
}
