using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Activities_Inspector.Services;
using Xunit;

namespace ActivitiesInspector.UnitTests.Services
{
    public class RecentFilesBeefStructuralTests
    {
        private static readonly Type Svc = typeof(RecentFilesService);

        private static string ParseBeef(byte[] itemData) =>
            (string)Svc.GetMethod("TryParseExtensionBlockBeef0004", BindingFlags.NonPublic | BindingFlags.Static,
                    null, new[] { typeof(byte[]), typeof(int) }, null)
                .Invoke(null, new object[] { itemData, 0 });

        private static string ParseShellItemPath(byte[] data) =>
            (string)Svc.GetMethod("ParseShellItemPath", BindingFlags.NonPublic | BindingFlags.Static,
                    null, new[] { typeof(byte[]) }, null)
                .Invoke(null, new object[] { data });

        private static string ParseRecentDocsValue(byte[] data) =>
            (string)Svc.GetMethod("ParseRecentDocsValue", BindingFlags.NonPublic | BindingFlags.Static)
                .Invoke(null, new object[] { data });

        /// <summary>
        /// Blocco 0xBEEF0004 secondo libfwsi. <paramref name="bytesBeforeName"/> sono gli ultimi 4 byte
        /// prima del nome (campo "unknown" per v8/v9) e permettono di simulare metadati che sembrano ASCII.
        /// </summary>
        private static byte[] BuildBeefBlock(string longName, ushort version = 9, byte[] bytesBeforeName = null,
            string localizedName = null)
        {
            var block = new List<byte>();
            block.AddRange(new byte[2]);                         // size (patch sotto)
            block.AddRange(BitConverter.GetBytes(version));
            block.AddRange(BitConverter.GetBytes(0xBEEF0004u));
            block.AddRange(new byte[8]);                         // ctime + atime
            block.AddRange(BitConverter.GetBytes((ushort)0x2E)); // identifier
            if (version >= 7) block.AddRange(new byte[2 + 8 + 8]);
            ushort lss = (ushort)(localizedName == null ? 0 : localizedName.Length + 1);
            block.AddRange(BitConverter.GetBytes(lss));          // long string size
            if (version >= 9) block.AddRange(new byte[4]);
            if (version >= 8) block.AddRange(new byte[4]);
            if (bytesBeforeName != null)
            {
                // sovrascrive gli ultimi byte prima del nome
                for (int i = 0; i < bytesBeforeName.Length; i++)
                    block[block.Count - bytesBeforeName.Length + i] = bytesBeforeName[i];
            }
            Assert.Equal(RecentFilesService.GetBeef0004LongNameOffset(version), block.Count);
            block.AddRange(Encoding.Unicode.GetBytes(longName + "\0"));
            if (localizedName != null) block.AddRange(Encoding.Unicode.GetBytes(localizedName + "\0"));
            block.AddRange(BitConverter.GetBytes((ushort)0x14)); // first extension block offset
            var bytes = block.ToArray();
            BitConverter.GetBytes((ushort)bytes.Length).CopyTo(bytes, 0);
            return bytes;
        }

        private static byte[] WrapInItem(byte[] block)
        {
            var item = new byte[12 + block.Length + 4];          // prefisso fittizio + padding finale
            block.CopyTo(item, 12);
            return item;
        }

        /// <summary>Shell item "file entry" (0x32) con short name ASCII e blocco BEEF, terminato da 00 00.</summary>
        private static byte[] BuildFileEntryIdList(string shortName, string longName)
        {
            var body = new List<byte> { 0x32, 0x00 };             // type, unknown
            body.AddRange(new byte[4 + 4 + 2]);                  // file size, mtime, attributes
            body.AddRange(Encoding.ASCII.GetBytes(shortName + "\0"));
            if (body.Count % 2 == 1) body.Add(0);
            body.AddRange(BuildBeefBlock(longName));
            var item = new List<byte>();
            item.AddRange(BitConverter.GetBytes((ushort)(body.Count + 2)));
            item.AddRange(body);
            item.AddRange(new byte[2]);                          // terminatore ID list
            return item.ToArray();
        }

        // --- Carattere spurio iniziale (metadati che sembrano ASCII) ---

        [Theory]
        [InlineData(0x55, "Report_17-9-2026.pdf")]
        [InlineData(0x47, "Progressive_Overload_Workout_Tracker.xlsx")]
        [InlineData(0x48, "Gumroad_Invoice.pdf")]
        public void Spurious_Leading_Char_From_Metadata_Is_Dropped(byte spurious, string expected)
        {
            var item = WrapInItem(BuildBeefBlock(expected, bytesBeforeName: new byte[] { 0xDC, 0x5F, spurious, 0x00 }));
            Assert.Equal(expected, ParseBeef(item));
        }

        [Theory]
        [InlineData("Monthly_Expense_Tracker_Dashboard_Template.xlsx")]
        [InlineData("UninstallView.txt")]
        public void Healthy_Block_Is_Unchanged(string expected)
        {
            Assert.Equal(expected, ParseBeef(WrapInItem(BuildBeefBlock(expected))));
        }

        // --- Nomi che la scansione euristica troncava o scartava ---

        [Theory]
        [InlineData("(1) Report.pdf")]
        [InlineData("[bozza] Relazione.docx")]
        [InlineData("~$Documento.docx")]
        [InlineData("!readme.txt")]
        [InlineData(".gitignore")]
        [InlineData("Отчёт за сентябрь.pdf")]
        [InlineData("报告_2026.xlsx")]
        [InlineData("レポート.pdf")]
        [InlineData("Σύμβαση.docx")]
        [InlineData("Übersicht Ärzte.pdf")]
        [InlineData("Été à Paris.jpg")]
        [InlineData("Ölwechsel 😀.txt")]
        public void Structural_Read_Preserves_Any_Valid_Filename(string expected)
        {
            Assert.Equal(expected, ParseBeef(WrapInItem(BuildBeefBlock(expected))));
        }

        [Theory]
        [InlineData("Отчёт за сентябрь.pdf")]
        [InlineData("报告_2026.xlsx")]
        [InlineData("(1) Report.pdf")]
        public void ShellItemPath_Keeps_International_Names(string expected)
        {
            Assert.Equal(expected, ParseShellItemPath(BuildFileEntryIdList("REPORT~1.PDF", expected)));
        }

        // --- Versioni del blocco (Windows 7 / Vista / XP / sconosciute) ---

        [Theory]
        [InlineData(3)]
        [InlineData(7)]
        [InlineData(8)]
        [InlineData(9)]
        public void All_Documented_Versions_Are_Read_Structurally(int version)
        {
            Assert.Equal("Отчёт.pdf", ParseBeef(WrapInItem(BuildBeefBlock("Отчёт.pdf", (ushort)version))));
        }

        [Fact]
        public void Unknown_Future_Version_Falls_Back_To_Heuristic()
        {
            // v10 non documentata: nessuna lettura strutturale, l'euristica trova comunque il nome ASCII
            Assert.Equal("Report.pdf", ParseBeef(WrapInItem(BuildBeefBlock("Report.pdf", 10))));
        }

        [Fact]
        public void Localized_Name_Does_Not_Leak_Into_Long_Name()
        {
            // Caso reale: cartella Documents con nome localizzato "@%SystemRoot%\...\shell32.dll,-21770"
            var item = WrapInItem(BuildBeefBlock("Documents", localizedName: @"@%SystemRoot%\system32\shell32.dll,-21770"));
            Assert.Equal("Documents", ParseBeef(item));
        }

        [Fact]
        public void Inconsistent_Block_Is_Not_Trusted()
        {
            // Terminatore anticipato senza nome localizzato: la struttura non torna, niente lettura strutturale
            var block = BuildBeefBlock("Report.pdf" + "\0" + "Junk");
            Assert.Null(RecentFilesService.TryReadStructuralLongName(block, 0, block.Length, 9));
        }

        [Fact]
        public void Garbage_At_Structural_Offset_Is_Rejected()
        {
            var block = BuildBeefBlock("Report.pdf");
            int nameStart = RecentFilesService.GetBeef0004LongNameOffset(9);
            block[nameStart] = 0x01; block[nameStart + 1] = 0x00;   // carattere di controllo
            Assert.Null(RecentFilesService.TryReadStructuralLongName(block, 0, block.Length, 9));
        }

        [Theory]
        [InlineData(2, -1)]
        [InlineData(3, 20)]
        [InlineData(7, 38)]
        [InlineData(8, 42)]
        [InlineData(9, 46)]
        public void LongName_Offset_Follows_Libfwsi_Layout(int version, int expected)
        {
            Assert.Equal(expected, RecentFilesService.GetBeef0004LongNameOffset((ushort)version));
        }

        // --- Validazione nomi ---

        [Theory]
        [InlineData("Отчёт.pdf", true)]
        [InlineData("报告.xlsx", true)]
        [InlineData("~$doc.docx", true)]
        [InlineData("ms-outlook:launch:calendar", false)]
        [InlineData("page?stkn=abc", false)]
        [InlineData("a\u0001b", false)]
        [InlineData("bad\uD800", false)]
        [InlineData("   ", false)]
        public void IsPlausibleFileName_Accepts_Any_Script_Rejects_Invalid(string name, bool expected)
        {
            Assert.Equal(expected, RecentFilesService.IsPlausibleFileName(name));
        }

        // --- RecentDocs ---

        [Theory]
        [InlineData("Отчёт за сентябрь.pdf")]
        [InlineData("报告_2026.xlsx")]
        [InlineData("Report_17-9-2026.pdf")]
        public void RecentDocs_Name_At_Offset_Zero_Is_Read_For_Any_Script(string expected)
        {
            var data = new List<byte>(Encoding.Unicode.GetBytes(expected + "\0"));
            data.AddRange(BuildFileEntryIdList("REPORT~1.LNK", expected + ".lnk"));
            Assert.Equal(expected, ParseRecentDocsValue(data.ToArray()));
        }

        [Fact]
        public void RecentDocs_Uri_Entries_Are_Still_Excluded()
        {
            var data = Encoding.Unicode.GetBytes("ms-outlook:launch:calendar\0\0\0");
            Assert.Null(ParseRecentDocsValue(data));
        }
    }
}
