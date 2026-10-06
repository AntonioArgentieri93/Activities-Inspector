using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Activities_Inspector.Models;
using Activities_Inspector.Services;
using Activities_Inspector.Utils;
using Activities_Inspector.ViewModels;
using Registry;
using Xunit;

namespace ActivitiesInspector.UnitTests.Services
{
    /// <summary>Correzioni USB emerse dal confronto con USBDeview: seriale vs ID istanza, nomi, abbinamento eventi, log di transazione.</summary>
    public class UsbCorrectnessTests
    {
        private static UsbEntry Make(string instanceId, string vid = "5986", string pid = "2127") =>
            new UsbEntry(false, "USB Video Device", instanceId, vid, pid, "Video");

        // --- Seriale reale o ID generato da Windows (secondo carattere '&') ---

        [Theory]
        [InlineData("MSFT30235678C218CA", "MSFT30235678C218CA")]   // chiavetta/disco: seriale vero
        [InlineData("9a269c0f773c", "9a269c0f773c")]                // lettore di impronte
        [InlineData("01.00.00", "01.00.00")]                        // composito con seriale
        [InlineData("6&12502cca&5&0000", "")]                       // interfaccia figlia: ID generato
        [InlineData("5&42af0bf&0&10", "")]                          // scheda Bluetooth: ID generato
        [InlineData("5&42af0bf&0&2", "")]
        [InlineData("", "")]
        [InlineData(null, "")]
        public void SerialNumber_Is_Shown_Only_When_Provided_By_The_Device(string instanceId, string expectedSerial)
        {
            var entry = Make(instanceId);

            Assert.Equal(instanceId, entry.InstanceId);
            Assert.Equal(expectedSerial, entry.SerialNumber);
        }

        [Fact]
        public void Entries_Differing_Only_By_Generated_Id_Are_Still_Distinct()
        {
            // Le tre interfacce del ricevitore Logitech (stesso VID/PID, seriale assente) non devono fondersi
            var first = Make("6&6169f22&0&0000", "046D", "C534");
            var second = Make("6&6169f22&0&0001", "046D", "C534");

            Assert.False(first.Equals(second));
            Assert.Equal(string.Empty, first.SerialNumber);
            Assert.Equal(string.Empty, second.SerialNumber);
        }

        // --- Nome del dispositivo ---

        [Theory]
        [InlineData("@usbvideo.inf,%usbvideo.devicedesc%;USB Video Device", "HD Camera", "USB Video Device (HD Camera)")]
        [InlineData("@usbvideo.inf,%usbvideo.devicedesc%;USB Video Device", "IR Camera", "USB Video Device (IR Camera)")]
        [InlineData("@input.inf,%hid_device%;USB Input Device", null, "USB Input Device")]
        [InlineData("@input.inf,%hid_device%;USB Input Device", "", "USB Input Device")]
        [InlineData("Intel(R) Wireless Bluetooth(R)", "Intel(R) Wireless Bluetooth(R)", "Intel(R) Wireless Bluetooth(R)")]  // identico: non duplicare
        [InlineData("", "Solo nome amichevole", "Solo nome amichevole")]
        public void Device_Name_Combines_Description_And_FriendlyName(string desc, string friendly, string expected)
        {
            Assert.Equal(expected, UsbTrackingService.BuildDeviceName(desc, friendly));
        }

        // --- Abbinamento eventi di collegamento ---

        [Fact]
        public void Plug_Event_Matches_Exact_Instance_Not_First_Same_VidPid()
        {
            // Regressione: con più voci con lo stesso VID/PID l'evento aggiornava sempre la prima
            var entries = new List<UsbEntry>
            {
                Make("6&12502cca&5&0000"),
                Make("6&12502cca&5&0002"),
                Make("01.00.00"),
            };

            var match = UsbViewModel.FindMatchingEntry(entries, "5986", "2127", "6&12502cca&5&0002");

            Assert.Same(entries[1], match);
        }

        [Fact]
        public void Plug_Event_Of_Unknown_Instance_Matches_Nothing()
        {
            var entries = new List<UsbEntry> { Make("6&12502cca&5&0000") };

            Assert.Null(UsbViewModel.FindMatchingEntry(entries, "5986", "2127", "6&12502cca&5&0009"));
            Assert.Null(UsbViewModel.FindMatchingEntry(null, "5986", "2127", "x"));
        }

        [Fact]
        public void Plug_Event_Match_Ignores_Case()
        {
            var entries = new List<UsbEntry> { Make("9a269c0f773c", "06CB", "009B") };

            Assert.NotNull(UsbViewModel.FindMatchingEntry(entries, "06cb", "009b", "9A269C0F773C"));
        }

        // --- Caricamento hive con transaction log ---

        private static string CopyImageHiveToTemp(out string dir)
        {
            var source = Path.Combine(Path.GetTempPath(), "FullImage", "Users", "Test", "AppData", "Local", "Microsoft", "Windows", "UsrClass.dat");
            if (!File.Exists(source))
                source = @"C:\Users\anton\OneDrive\Desktop\FullImage\Users\Test\AppData\Local\Microsoft\Windows\UsrClass.dat";
            if (!File.Exists(source))
                throw new Xunit.Sdk.SkipException("Hive di prova assente — test saltato");

            dir = Path.Combine(Path.GetTempPath(), "hive_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            var copy = Path.Combine(dir, "UsrClass.dat");
            File.Copy(source, copy);
            return copy;
        }

        [Fact]
        public void Clean_Hive_Is_Parsed_Once_Without_Logs()
        {
            var path = CopyImageHiveToTemp(out var dir);
            try
            {
                var hive = OfflineHiveLoader.Load(File.ReadAllBytes(path), path, new List<TransactionLogFileInfo>(), out var applied);

                Assert.False(applied);
                Assert.NotNull(hive.Root);   // già analizzato: una seconda ParseHive darebbe "ParseHive already called"
                Assert.NotEmpty(hive.Root.SubKeys);
            }
            finally { Directory.Delete(dir, true); }
        }

        [Fact]
        public void Dirty_Hive_With_Unusable_Logs_Falls_Back_To_The_Hive_As_Is()
        {
            var path = CopyImageHiveToTemp(out var dir);
            try
            {
                var bytes = File.ReadAllBytes(path);
                // Rende l'hive "sporco": PrimarySequenceNumber (offset 4) != SecondarySequenceNumber (offset 8)
                BitConverter.GetBytes(BitConverter.ToUInt32(bytes, 4) + 1).CopyTo(bytes, 4);
                var garbage = new List<TransactionLogFileInfo> { new TransactionLogFileInfo("UsrClass.dat.LOG1", new byte[4096]) };

                var hive = OfflineHiveLoader.Load(bytes, path, garbage, out _);

                // Qualunque sia l'esito dei log, il risultato è un hive utilizzabile e analizzato una sola volta
                Assert.NotNull(hive.Root);
                Assert.NotEmpty(hive.Root.SubKeys);
            }
            finally { Directory.Delete(dir, true); }
        }

        [Fact]
        public void Log_Files_Are_Found_Next_To_The_Hive()
        {
            var dir = Path.Combine(Path.GetTempPath(), "usb_logs_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            try
            {
                var hive = Path.Combine(dir, "SYSTEM");
                File.WriteAllBytes(hive, new byte[] { 1 });
                File.WriteAllBytes(hive + ".LOG1", new byte[] { 1 });
                File.WriteAllBytes(hive + ".LOG2", new byte[] { 1 });

                var logs = OfflineHiveLoader.FindTransactionLogs(hive).Select(Path.GetFileName).ToList();

                Assert.Equal(new[] { "SYSTEM.LOG1", "SYSTEM.LOG2" }, logs);
            }
            finally { Directory.Delete(dir, true); }
        }

        // --- CSV ---

        [Fact]
        public void Csv_Row_Has_Serial_And_InstanceId_Columns_Matching_The_Header()
        {
            var entry = new UsbEntry(true, "Intel(R) Wireless Bluetooth(R)", "5&42af0bf&0&10", "8087", "0026", "Wireless Controller");
            var path = Path.Combine(Path.GetTempPath(), "usb_csv_" + Guid.NewGuid().ToString("N") + ".csv");
            try
            {
                using (var writer = new EntryWriter(path, false, new System.Text.UTF8Encoding(false), new EntryFormatter()))
                    writer.WriteEntries(new[] { entry }, EntryType.Usb);

                var lines = File.ReadAllLines(path);
                var header = lines[0].Split(new[] { " ; " }, StringSplitOptions.None);
                var row = lines[1].Split(new[] { " ; " }, StringSplitOptions.None);

                Assert.Equal(header.Length, row.Length);
                Assert.Equal("Serial number", header[2]);
                Assert.Equal("ID istanza", header[3]);
                Assert.Equal(string.Empty, row[2]);                 // nessun seriale hardware
                Assert.Equal("5&42af0bf&0&10", row[3]);             // ID generato da Windows
            }
            finally { File.Delete(path); File.Delete(path + ".sha256"); }
        }
    }
}
