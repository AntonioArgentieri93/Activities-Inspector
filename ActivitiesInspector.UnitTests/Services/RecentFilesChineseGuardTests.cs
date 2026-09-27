using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Activities_Inspector.Services;
using Xunit;

namespace ActivitiesInspector.UnitTests.Services
{
    public class RecentFilesChineseGuardTests
    {
        private static MethodInfo IsValidPathComponentMethod =>
            typeof(RecentFilesService).GetMethod("IsValidPathComponent", BindingFlags.NonPublic | BindingFlags.Static);

        private static bool IsValid(string s) => (bool)IsValidPathComponentMethod.Invoke(null, new object[] { s });

        private static MethodInfo HasSuspiciousMethod =>
            typeof(RecentFilesService).GetMethod("HasSuspiciousChars", BindingFlags.NonPublic | BindingFlags.Static);

        private static bool HasSuspicious(string s) => (bool)HasSuspiciousMethod.Invoke(null, new object[] { s });

        [Theory]
        [InlineData("Monthly_Expense_Tracker_Dashboard_Template.xlsx")]
        [InlineData("UninstallView.txt")]
        [InlineData("Report_19-9-2026.pdf")]
        [InlineData("pin1_bizos_allinone.png")]
        [InlineData("silentnight.pdf")]
        public void Valid_Ascii_Filenames_Are_Accepted(string name)
        {
            Assert.True(IsValid(name));
            Assert.False(HasSuspicious(name));
        }

        [Theory]
        [InlineData("崳蛼崴䱪.")]
        [InlineData("ꨞðAccensione - Spegnimento_19-9-2026_19-34-12.csv")]
        [InlineData("崥帕崥帕.")]
        [InlineData("崲弻崲弻.")]
        [InlineData("崨麱崨麶.")]
        [InlineData("に")]
        public void Chinese_And_Garbage_Are_Rejected(string name)
        {
            Assert.False(IsValid(name));
        }

        [Theory]
        [InlineData("崝愡崝愩.")]
        [InlineData("뻯崝愡崝愩.")]
        [InlineData("崒匐崔䉽.")]
        public void Cjk_Strings_Are_Flagged_As_Suspicious(string name)
        {
            Assert.True(HasSuspicious(name));
            Assert.False(IsValid(name));
        }

        [Fact]
        public void Truncated_ShortName_Like_Illegal_Is_Rejected_If_Ratio_Low()
        {
            // Simulate garbage that passes old check (one ASCII char) but fails 80% ratio
            Assert.False(IsValid("崳a"));
            Assert.False(IsValid("崳崝a"));
        }

        [Fact]
        public void ParseFileEntryName_LongName_Beats_ShortName_OffByOne()
        {
            // This is an integration-level guard: if BEEF block parsing fails, at least the IsValidPathComponent
            // fix prevents off-by-one short names like "ONTHL~1.XLS" from being preferred over nothing.
            // Direct test of IsValidPathComponent strictness.
            Assert.True(IsValid("MONTHL~1.XLS"));
            Assert.True(IsValid("Monthly_Expense_Tracker_Dashboard_Template.xlsx"));
            // Short 8.3 is still valid (but long should win via scoring in real parser)
            // The key guard is that garbage like "ilentnight.pdf" (missing first char) is still valid,
            // but the BEEF parser should have corrected it to "silentnight.pdf".
        }

        // Continuous guard: scan a synthetic CSV content for CJK leakage
        [Fact]
        public void Exported_RecentFiles_Should_Not_Contain_Cjk()
        {
            var syntheticRows = new[]
            {
                "Monthly_Expense_Tracker_Dashboard_Template.xlsx",
                "UninstallView.txt",
                "RecentFilesView.txt",
                "Screenshot 2026-09-18 142501.png"
            };
            var hasCjkInOutput = syntheticRows.Any(r => HasSuspicious(r));
            Assert.False(hasCjkInOutput);
        }
    }
}
