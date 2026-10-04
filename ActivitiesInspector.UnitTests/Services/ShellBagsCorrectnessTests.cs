using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Activities_Inspector.Models;
using Activities_Inspector.Services;
using Activities_Inspector.Utils;
using Activities_Inspector.ViewModels;
using Xunit;

namespace ActivitiesInspector.UnitTests.Services
{
    /// <summary>Correzioni ShellBags: nomi, percorsi, date, chiavi lette, transaction log.</summary>
    public class ShellBagsCorrectnessTests
    {
        private static byte[] Hex(string hex) =>
            hex.Split(new[] { ' ', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Select(h => Convert.ToByte(h, 16)).ToArray();

        private static IShellItem ParseItem(byte[] raw) => new ShellItemList(raw).Items().First();

        private static RegistryShellItemDecorator Decorate(byte[] raw, IShellItem parent = null) =>
            new RegistryShellItemDecorator(ParseItem(raw), new RegistryKeyWrapper(raw), parent);

        // --- Valori reali da BagMRU (registro di questo PC) ---

        // BagMRU\19: root 0x1F "Users property view: drive letter" con "/E:\" (unità esterna)
        private static readonly byte[] DriveLetterRoot = Hex(
            "55 00 1F 00 2F 00 10 B7 A6 F5 19 00 2F 45 3A 5C 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 " +
            "00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 74 1A 59 5E 96 DF D3 48 8D 67 17 " +
            "33 BC EE 28 BA 77 2C FB F5 2F 0E 16 4A A3 81 3E 56 0C 68 BC 83 00 00");

        // BagMRU\2\25: item 0x2E con GUID {374DE290-…} (Downloads) all'offset 4
        private static readonly byte[] DownloadsGuidItem = Hex(
            "3A 00 2E 48 90 E2 4D 37 3F 12 65 45 91 64 39 C4 92 5E 46 7B 26 00 01 00 26 00 EF BE 11 00 00 00 " +
            "F1 C3 E7 1F 2E E1 D7 01 D7 5A 71 76 85 CC DA 01 36 B4 86 78 85 CC DA 01 14 00 00 00");

        // BagMRU\2\11 (inizio): dispositivo MTP "HUAWEI P20 Pro"
        private static readonly byte[] MtpDevice = Hex(
            "92 01 2E 00 6C 01 06 20 31 08 03 00 00 00 00 00 00 00 03 00 00 00 7A 00 00 00 01 00 00 00 0F 00 " +
            "00 00 58 00 00 00 00 00 48 00 55 00 41 00 57 00 45 00 49 00 20 00 50 00 32 00 30 00 20 00 50 00 " +
            "72 00 6F 00 00 00 5C 00 5C 00 3F 00 5C 00 75 00");

        [Fact]
        public void Drive_Letter_Root_Item_Is_Resolved()
        {
            Assert.Equal(@"E:\", Decorate(DriveLetterRoot).Name);
        }

        [Fact]
        public void Guid_Folder_0x2E_Is_Resolved_To_Known_Folder()
        {
            Assert.Equal("Downloads", RegistryShellItemDecorator.TryDecodeGuidFolder(DownloadsGuidItem));
            Assert.Equal("Downloads", Decorate(DownloadsGuidItem).Name);
        }

        [Fact]
        public void Mtp_Device_Name_Is_Decoded()
        {
            Assert.Equal("HUAWEI P20 Pro", RegistryShellItemDecorator.TryDecodeMtpDevice(MtpDevice));
        }

        // --- Percorsi ---

        private sealed class FakeParent : IShellItem
        {
            private readonly string _path;
            public FakeParent(string path) => _path = path;
            public ushort Size => 0;
            public byte Type => 0x1F;
            public string TypeName => "Root";
            public string Name => _path;
            public DateTime ModifiedDate => DateTime.MinValue;
            public DateTime AccessedDate => DateTime.MinValue;
            public DateTime CreationDate => DateTime.MinValue;
            public IDictionary<string, string> GetAllProperties() => new Dictionary<string, string> { ["AbsolutePath"] = _path };
        }

        [Theory]
        [InlineData("Documents", "Spartiti", @"Documents\Spartiti")]                      // prima: "Spartiti" (radice scartata)
        [InlineData(@"My Computer\C:\", "Users", @"My Computer\C:\Users")]
        [InlineData(@"E:\", "Lavoro", @"E:\Lavoro")]
        [InlineData(@"Documents", "{Bozza}", @"Documents\{Bozza}")]                       // prima: segmento con { scartato
        public void Absolute_Path_Keeps_Every_Segment(string parent, string name, string expected)
        {
            Assert.Equal(expected, RegistryShellItemDecorator.BuildAbsolutePath(new FakeParent(parent), name));
        }

        // --- File entry 0x31: nome lungo/corto ---

        private static byte[] FileEntry(string shortName, string longName, uint signature = 0xBEEF0004)
        {
            var item = new List<byte> { 0, 0, 0x31, 0x00 };
            item.AddRange(new byte[4]);                                   // file size
            item.AddRange(new byte[] { 0x21, 0x5A, 0x43, 0x5B });         // DOS date/time
            item.AddRange(BitConverter.GetBytes((ushort)0x10));          // attributes
            item.AddRange(Encoding.ASCII.GetBytes(shortName + "\0"));
            if (item.Count % 2 == 1) item.Add(0);

            if (longName != null)
            {
                int blockStart = item.Count;
                var block = new List<byte>();
                block.AddRange(new byte[2]);
                block.AddRange(BitConverter.GetBytes((ushort)9));
                block.AddRange(BitConverter.GetBytes(signature));
                block.AddRange(new byte[8]);                              // ctime + atime
                block.AddRange(BitConverter.GetBytes((ushort)0x2E));
                block.AddRange(new byte[2 + 8 + 8 + 2 + 4 + 4]);
                block.AddRange(Encoding.Unicode.GetBytes(longName + "\0"));
                block.AddRange(BitConverter.GetBytes((ushort)blockStart)); // offset del blocco nell'item
                var b = block.ToArray();
                BitConverter.GetBytes((ushort)b.Length).CopyTo(b, 0);
                item.AddRange(b);
            }
            else
            {
                item.AddRange(new byte[2]);                               // item XP: nessun blocco
            }

            var bytes = item.ToArray();
            BitConverter.GetBytes((ushort)bytes.Length).CopyTo(bytes, 0);
            return bytes.Concat(new byte[2]).ToArray();                   // terminatore ID list
        }

        [Fact]
        public void File_Entry_Uses_Long_Name_From_Valid_Block()
        {
            Assert.Equal("Spartiti per sax", ParseItem(FileEntry("SPARTI~1", "Spartiti per sax")).Name);
        }

        [Fact]
        public void File_Entry_Without_Extension_Block_Uses_Short_Name()
        {
            // Prima: blocco letto comunque da un offset casuale → nome vuoto o spazzatura
            Assert.Equal("SPARTI~1", ParseItem(FileEntry("SPARTI~1", null)).Name);
        }

        [Fact]
        public void File_Entry_With_Wrong_Block_Signature_Uses_Short_Name()
        {
            Assert.Equal("SPARTI~1", ParseItem(FileEntry("SPARTI~1", "Spartiti", signature: 0xBEEF0026)).Name);
        }

        [Fact]
        public void File_Entry_Modified_Date_Is_Exported_As_Local_Time()
        {
            var entry = ShellBagsViewModel.GetShellBagsEntries(new List<IShellItem> { Decorate(FileEntry("SPARTI~1", "Spartiti")) }).Single();

            // DOS 0x5A21 / 0x5B43 = 2025-01-01 11:26:06 UTC
            var expected = DateTime.SpecifyKind(new DateTime(2025, 1, 1, 11, 26, 6), DateTimeKind.Utc).ToLocalTime();
            Assert.Equal(expected, entry.ModifiedOn);
        }

        // --- Date di interazione ---

        [Theory]
        [InlineData(new byte[] { 3, 0, 0, 0, 0, 0, 0, 0, 0xFF, 0xFF, 0xFF, 0xFF }, 3)]
        [InlineData(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }, null)]
        [InlineData(new byte[0], null)]
        public void Mru_Most_Recent_Index(byte[] mru, int? expected)
        {
            Assert.Equal(expected, RegistryKeyWrapper.MostRecentIndex(mru));
        }

        [Fact]
        public void Entry_Carries_Last_Interacted_And_Csv_Shows_NA_When_Missing()
        {
            var raw = FileEntry("SPARTI~1", "Spartiti");
            var withDate = new RegistryKeyWrapper(raw) { LastInteracted = new DateTime(2026, 10, 4, 15, 0, 0) };
            var entries = ShellBagsViewModel.GetShellBagsEntries(new List<IShellItem>
            {
                new RegistryShellItemDecorator(ParseItem(raw), withDate),
                new RegistryShellItemDecorator(ParseItem(raw), new RegistryKeyWrapper(raw)),
            }).ToList();

            Assert.Equal(new DateTime(2026, 10, 4, 15, 0, 0), entries[0].LastInteracted);
            Assert.Null(entries[1].LastInteracted);

            var csv = new EntryFormatter().AsCsv(entries[1]).Split(new[] { " ; " }, StringSplitOptions.None);
            Assert.Equal(7, csv.Length);
            Assert.Equal(DateBuilder.NotAvailable, csv[1]);   // Ultima interazione
            Assert.Equal(DateBuilder.NotAvailable, csv[2]);   // Ultima scrittura chiave
        }

        // --- Chiavi lette ---

        [Fact]
        public void Registry_Locations_Cover_NtUser_And_UsrClass_Without_Bags()
        {
            var locations = new ConfigParser().GetRegistryLocations();

            Assert.Contains(@"Software\Microsoft\Windows\Shell\BagMRU", locations);
            Assert.Contains(@"Local Settings\Software\Microsoft\Windows\Shell\BagMRU", locations);
            Assert.Contains(@"Software\Microsoft\Windows\ShellNoRoam\BagMRU", locations);
            Assert.All(locations, l => Assert.EndsWith(@"\BagMRU", l));
        }

        // --- Transaction log ---

        [Fact]
        public void Transaction_Logs_Next_To_Hive_Are_Found()
        {
            var dir = Path.Combine(Path.GetTempPath(), "sb_logs_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            try
            {
                var hive = Path.Combine(dir, "UsrClass.dat");
                File.WriteAllBytes(hive, new byte[] { 1 });
                File.WriteAllBytes(hive + ".LOG1", new byte[] { 1 });
                File.WriteAllBytes(hive + ".LOG2", new byte[] { 1 });
                File.WriteAllBytes(hive + ".LOG", new byte[0]);          // vuoto: escluso
                File.WriteAllBytes(hive + ".bak", new byte[] { 1 });     // non è un log

                var logs = OfflineHiveLoader.FindTransactionLogs(hive).Select(Path.GetFileName).ToList();

                Assert.Equal(new[] { "UsrClass.dat.LOG1", "UsrClass.dat.LOG2" }, logs);
            }
            finally
            {
                Directory.Delete(dir, true);
            }
        }
    }
}
