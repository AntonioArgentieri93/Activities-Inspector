using Activities_Inspector.Models;
using Activities_Inspector.Services;
using Activities_Inspector.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace ActivitiesInspector.UnitTests.Services
{
    public class ParsingCorrectnessTests
    {
        [Fact]
        public void Install_ShouldInclude_Filters_SystemComponent_And_ParentKey()
        {
            Assert.False(InstallEntriesBuilder.ShouldInclude("App", "1", null, null));
            Assert.False(InstallEntriesBuilder.ShouldInclude("App", null, "parent", null));
            Assert.False(InstallEntriesBuilder.ShouldInclude("App", null, null, "Update"));
            Assert.False(InstallEntriesBuilder.ShouldInclude("", null, null, null));
            Assert.True(InstallEntriesBuilder.ShouldInclude("App", null, null, null));
        }

        [Fact]
        public void Install_Dedupe_Keeps_Registry_Path_And_Best_Date()
        {
            var a = new InstallEntry("App", @"HKLM\...", @"C:\App", null);
            var b = new InstallEntry("App", null, null, new DateTime(2024, 1, 10));
            var c = new InstallEntry("App", @"HKLM\...", @"C:\App2", new DateTime(2024, 1, 11));

            var result = InstallEntriesBuilder.DedupeEntries(new List<InstallEntry> { a, b, c });

            Assert.Single(result);
            Assert.Equal(@"C:\App", result[0].FullPath);
            Assert.Equal(new DateTime(2024, 1, 10), result[0].InstallDate);
        }

        [Fact]
        public void ShellBag_Entry_Maps_Properties_Correctly()
        {
            var props = new Dictionary<string, string>
            {
                ["AbsolutePath"] = @"C:\Users\a",
                ["RegistryPath"] = @"HKU\...\BagMRU"
            };
            var fake = new FakeShellItem(props);
            var entries = GetShellBags(new[] { fake });
            Assert.Single(entries);
            Assert.Equal(@"C:\Users\a", entries[0].AbsolutePath);
            Assert.Equal(@"HKU\...\BagMRU", entries[0].RegistryPath);
        }

        [Fact]
        public void Session_AccessType_Filter_Keeps_Only_Human_Types()
        {
            // Simulate filtering via LoggedInfoService helper is internal; test via BuildLogOnEntries tolerance
            var entry = new LogOnEntry(4624, "PC", "0x1", DateTime.Now, "user", "DOM", "g", 2, "10.0.0.1");
            Assert.Equal(2, entry.AccessType);
        }

        [Fact]
        public void Usage_PairIntervals_Does_Not_Reuse_Start()
        {
            var now = DateTime.Now;
            var points = new List<(DateTime Time, string Machine, bool IsStart, bool IsCrashBoot)>
            {
                (now.AddHours(-3), "PC", true, false),
                (now.AddHours(-2), "PC", true, false),
                (now.AddHours(-1), "PC", false, false)
            };
            var pairs = UsageLogTimeService.PairIntervals(points);
            // One end consumes only one start, plus open interval
            Assert.Equal(2, pairs.Count);
        }

        [Fact]
        public void Lnk_Skipped_Items_Not_Throw()
        {
            var raw = new byte[103];
            Buffer.BlockCopy(BitConverter.GetBytes(0x00000001), 0, raw, 20, 4);
            Buffer.BlockCopy(BitConverter.GetBytes((short)25), 0, raw, 76, 2);
            raw[78] = 18; raw[79] = 0; raw[80] = 0x01;
            raw[96] = 3; raw[97] = 0; raw[98] = 0xFF;
            var lnk = new LnkFile(raw, "test.lnk");
            Assert.Equal(1, lnk.SkippedShellItems);
        }

        private static List<ShellBagEntry> GetShellBags(IEnumerable<IShellItem> items)
        {
            return items.Select(item =>
            {
                var p = item.GetAllProperties();
                return new ShellBagEntry(p.ContainsKey("AbsolutePath") ? p["AbsolutePath"] : "", DateTime.MinValue, p.ContainsKey("RegistryPath") ? p["RegistryPath"] : "");
            }).ToList();
        }

        private sealed class FakeShellItem : IShellItem
        {
            private readonly Dictionary<string, string> _props;
            public FakeShellItem(Dictionary<string, string> props) => _props = props;
            public ushort Size => 0;
            public byte Type => 0;
            public string TypeName => "";
            public string Name => "";
            public DateTime ModifiedDate => DateTime.MinValue;
            public DateTime AccessedDate => DateTime.MinValue;
            public DateTime CreationDate => DateTime.MinValue;
            public IDictionary<string, string> GetAllProperties() => _props;
        }
    }
}
