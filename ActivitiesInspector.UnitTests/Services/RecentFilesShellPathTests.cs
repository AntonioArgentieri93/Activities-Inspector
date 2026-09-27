using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Activities_Inspector.Models;
using Activities_Inspector.Services;
using Xunit;

namespace ActivitiesInspector.UnitTests.Services
{
    /// <summary>Radici delle cartelle note, item volume e unione RecentDocs ↔ .lnk.</summary>
    public class RecentFilesShellPathTests
    {
        private static string Parse(byte[] idList, IReadOnlyDictionary<string, string> shellFolders = null) =>
            (string)typeof(RecentFilesService)
                .GetMethod("ParseShellItemPath", BindingFlags.NonPublic | BindingFlags.Static,
                    null, new[] { typeof(byte[]), typeof(IReadOnlyDictionary<string, string>) }, null)
                .Invoke(null, new object[] { idList, shellFolders });

        private static readonly Dictionary<string, string> ShellFolders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Personal"] = @"C:\Users\anton\OneDrive\Documents",
            ["Desktop"] = @"C:\Users\anton\OneDrive\Desktop",
            ["{374DE290-123F-4565-9164-39C4925E467B}"] = @"C:\Users\anton\Downloads",
        };

        private static byte[] RootItem(string clsid)
        {
            var item = new List<byte> { 0x14, 0x00, 0x1F, 0x50 };  // size 20, type, sort index
            item.AddRange(new Guid(clsid).ToByteArray());
            return item.ToArray();
        }

        // Item volume reale dall'hive: 19-00-2F-43-3A-5C-00...
        private static byte[] VolumeItem(string drive)
        {
            var item = new byte[25];
            item[0] = 25; item[2] = 0x2F;
            Encoding.ASCII.GetBytes(drive).CopyTo(item, 3);
            return item;
        }

        private static byte[] FileItem(string longName, byte type = 0x31)
        {
            var body = new List<byte> { type, 0x00 };
            body.AddRange(new byte[10]);
            body.AddRange(Encoding.ASCII.GetBytes("SHORT~1\0"));
            var block = new List<byte>();
            block.AddRange(new byte[2]);
            block.AddRange(BitConverter.GetBytes((ushort)9));
            block.AddRange(BitConverter.GetBytes(0xBEEF0004u));
            block.AddRange(new byte[8]);
            block.AddRange(BitConverter.GetBytes((ushort)0x2E));
            block.AddRange(new byte[18 + 2 + 4 + 4]);
            block.AddRange(Encoding.Unicode.GetBytes(longName + "\0"));
            block.AddRange(BitConverter.GetBytes((ushort)0x14));
            var b = block.ToArray();
            BitConverter.GetBytes((ushort)b.Length).CopyTo(b, 0);
            body.AddRange(b);
            var item = new List<byte>();
            item.AddRange(BitConverter.GetBytes((ushort)(body.Count + 2)));
            item.AddRange(body);
            return item.ToArray();
        }

        private static byte[] IdList(params byte[][] items)
        {
            var l = new List<byte>();
            foreach (var i in items) l.AddRange(i);
            l.AddRange(new byte[2]);
            return l.ToArray();
        }

        [Fact]
        public void Documents_Root_Resolves_To_Shell_Folder_Path()
        {
            var idl = IdList(RootItem("d3162b92-9365-467a-956b-92703aca08af"),
                FileItem("Spartiti"), FileItem("Sax"), FileItem("silentnight.pdf", 0x32));
            Assert.Equal(@"C:\Users\anton\OneDrive\Documents\Spartiti\Sax\silentnight.pdf", Parse(idl, ShellFolders));
        }

        [Theory]
        [InlineData("088e3905-0323-4b02-9826-5d99428e115f", @"C:\Users\anton\Downloads\a.png")]  // Questo PC\Download
        [InlineData("374de290-123f-4565-9164-39c4925e467b", @"C:\Users\anton\Downloads\a.png")]  // Download legacy
        [InlineData("b4bfcc3a-db2c-424c-b029-7fe99a87c641", @"C:\Users\anton\OneDrive\Desktop\a.png")]
        [InlineData("a8cdff1c-4878-43be-b5fd-f8091c1c60d0", @"C:\Users\anton\OneDrive\Documents\a.png")]
        public void Known_Folder_Roots(string clsid, string expected)
        {
            Assert.Equal(expected, Parse(IdList(RootItem(clsid), FileItem("a.png", 0x32)), ShellFolders));
        }

        [Fact]
        public void Root_Alone_Is_The_Folder_Itself()
        {
            Assert.Equal(@"C:\Users\anton\OneDrive\Desktop",
                Parse(IdList(RootItem("b4bfcc3a-db2c-424c-b029-7fe99a87c641")), ShellFolders));
        }

        [Fact]
        public void Without_Shell_Folders_Falls_Back_To_Generic_Name()
        {
            var idl = IdList(RootItem("d3162b92-9365-467a-956b-92703aca08af"), FileItem("a.pdf", 0x32));
            Assert.Equal(@"Documents\a.pdf", Parse(idl, null));
        }

        [Fact]
        public void This_Pc_Plus_Volume_Keeps_Drive_Letter()
        {
            var idl = IdList(RootItem("20d04fe0-3aea-1069-a2d8-08002b30309d"), VolumeItem(@"C:\"),
                FileItem("Users"), FileItem("anton"), FileItem("OfflineKit"));
            Assert.Equal(@"C:\Users\anton\OfflineKit", Parse(idl, ShellFolders));
        }

        [Fact]
        public void Volume_Alone_Is_Drive_Root()
        {
            var idl = IdList(RootItem("20d04fe0-3aea-1069-a2d8-08002b30309d"), VolumeItem(@"E:\"));
            Assert.Equal(@"E:\", Parse(idl, ShellFolders));
        }

        [Fact]
        public void File_Entry_Without_Root_Is_Relative_To_Desktop()
        {
            // Caso reale OpenSavePidlMRU\reg[0]: solo l'item "test.reg" (salvato sul Desktop)
            Assert.Equal(@"C:\Users\anton\OneDrive\Desktop\test.reg",
                Parse(IdList(FileItem("test.reg", 0x32)), ShellFolders));
        }

        [Fact]
        public void File_Entry_Without_Root_And_No_Shell_Folders_Is_Unchanged()
        {
            Assert.Equal("test.reg", Parse(IdList(FileItem("test.reg", 0x32)), null));
        }

        // Caso reale OpenSavePidlMRU\*[4]: {59031a47} profilo → item 0x00 (0x23FEBBEE, FOLDERID_SkyDrive) → Documents\...
        private static byte[] KnownFolderItem(string folderId)
        {
            var item = new List<byte> { 0x20, 0x00, 0x00, 0x00, 0x1A, 0x00 };
            item.AddRange(BitConverter.GetBytes(0x23FEBBEEu));
            item.AddRange(new byte[] { 0x00, 0x00, 0x10, 0x00 });
            item.AddRange(new Guid(folderId).ToByteArray());
            item.AddRange(new byte[2]);
            return item.ToArray();
        }

        [Fact]
        public void User_Profile_Then_OneDrive_Known_Folder_Resolves_To_OneDrive_Path()
        {
            var folders = new Dictionary<string, string>(ShellFolders, StringComparer.OrdinalIgnoreCase)
            {
                ["*UserProfile"] = @"C:\Users\anton",
                ["*OneDrive"] = @"C:\Users\anton\OneDrive",
            };
            var idl = IdList(RootItem("59031a47-3f72-44a7-89c5-5595fe6b30ee"),
                KnownFolderItem("a52bba46-e9e1-435f-b3d9-28daa648c0f6"),
                FileItem("Documents"), FileItem("Activities Inspector"));
            Assert.Equal(@"C:\Users\anton\OneDrive\Documents\Activities Inspector", Parse(idl, folders));
        }

        // --- RecentDocs ↔ .lnk ---

        private static byte[] RecentDocsValue(string name, string lnkName)
        {
            var l = new List<byte>(Encoding.Unicode.GetBytes(name + "\0"));
            l.AddRange(IdList(FileItem(lnkName, 0x32)));
            return l.ToArray();
        }

        [Fact]
        public void RecentDocs_Value_Exposes_Referenced_Lnk_Name()
        {
            Assert.Equal("Report_17-9-2026.pdf.lnk",
                RecentFilesService.ExtractRecentDocsLnkName(RecentDocsValue("Report_17-9-2026.pdf", "Report_17-9-2026.pdf.lnk")));
        }

        [Fact]
        public void RecentDocs_Entries_With_Existing_Lnk_Are_Merged_Others_Kept()
        {
            var lnk = new List<RecentFolderEntry>
            {
                new RecentFolderEntry(DateTime.Now, "Report", @"X:\Recent\Report.pdf.lnk", @"C:\Users\a\Report.pdf")
            };
            var reg = new List<RecentFolderEntry>
            {
                new RecentFolderEntry(DateTime.Now, "Report.pdf", @"NTUSER.DAT\...\RecentDocs\.pdf", "Report.pdf") { LinkedLnkName = "report.PDF.lnk" },
                new RecentFolderEntry(DateTime.Now, "Gone.pdf", @"NTUSER.DAT\...\RecentDocs\.pdf", "Gone.pdf") { LinkedLnkName = "Gone.pdf.lnk" },
                new RecentFolderEntry(DateTime.Now, "a.png", @"NTUSER.DAT\...\OpenSavePidlMRU\png", @"C:\a.png"),
            };

            RecentFilesService.MergeRecentDocsIntoLnk(reg, lnk);

            Assert.Equal(new[] { "Gone.pdf", "a.png" }, reg.ConvertAll(e => e.FileName));
        }
    }
}
