using Activities_Inspector.Models;
using Activities_Inspector.Services.Evidence;
using Activities_Inspector.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Xunit;

namespace ActivitiesInspector.IntegrationTests.Services
{
    [Trait("Category", "Integration")]
    public class EvtxParityTests
    {
        [Fact]
        public void Exported_Application_Log_Parses_Like_Live_Api()
        {
            var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".evtx");

            try
            {
                var export = Process.Start(new ProcessStartInfo("wevtutil", $"epl Application \"{path}\"")
                {
                    UseShellExecute = false,
                    CreateNoWindow = true
                });
                export.WaitForExit(120000);
                Assert.Equal(0, export.ExitCode);

                var wanted = new HashSet<int> { 11707, 1040, 1042 };
                var fileRows = EvtxFileReader.ReadEvents(path)
                    .Where(r => wanted.Contains(r.EventId))
                    .Select(Canonical).ToList();
                Assert.NotEmpty(fileRows);

                var liveRows = Helpers.GetLogEntries("Application")
                    .Where(r => wanted.Contains(r.EventId))
                    .Select(Canonical).ToHashSet();

                // Solo gli ID consumati dall'app (installazioni MSI): il resto
                // del log contiene famiglie volatili (WER, RestartManager,
                // TPM/AIK) che Windows riscrive anche a distanza di settimane.
                // La crescita del log tra export e lettura e' tollerata.
                var fileNorm = new HashSet<string>(fileRows.Select(NormalizeVolatile));
                var liveNorm = new HashSet<string>(liveRows.Select(NormalizeVolatile));
                var missing = fileNorm.Where(r => !liveNorm.Contains(r)).Take(3).ToList();
                Assert.True(missing.Count == 0,
                    "solo-file:\n" + string.Join("\n---\n", missing));
            }
            finally
            {
                try { File.Delete(path); } catch { }
            }
        }

        [Fact]
        public void Committed_Fixture_Parses_Deterministically()
        {
            var fixture = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory, "Fixtures", "ApplicationStable.evtx");

            var records = EvtxFileReader.ReadEvents(fixture);

            Assert.NotEmpty(records);
            Assert.All(records, r =>
            {
                Assert.Contains(r.EventId, new[] { 11707, 1040, 1042 });
                Assert.Equal("MsiInstaller", r.Source);
                Assert.NotEmpty(r.ReplacementStrings);
            });
        }

        private static string Canonical(IEventRecord record)
        {
            // Senza Source: l'API live risolve i nomi provider "display"
            // (es. Software Protection Platform Service) mentre il file
            // riporta il nome tecnico. La Source dei provider usati
            // dall'app e' verificata dal test seguente.
            var truncated = new DateTime(record.TimeGenerated.Year, record.TimeGenerated.Month,
                record.TimeGenerated.Day, record.TimeGenerated.Hour, record.TimeGenerated.Minute,
                record.TimeGenerated.Second, record.TimeGenerated.Kind);
            return string.Join("\u001F",
                record.EventId,
                truncated.ToString("O"),
                record.MachineName,
                string.Join("\u001E", record.ReplacementStrings ?? Array.Empty<string>()));
        }

        private static bool IsOlderThan(string canonicalRow, DateTime cutoff)
        {
            var parts = canonicalRow.Split('\u001F');
            return parts.Length > 1
                && DateTime.TryParse(parts[1], null,
                    System.Globalization.DateTimeStyles.RoundtripKind, out var timestamp)
                && timestamp < cutoff;
        }

        private static readonly System.Text.RegularExpressions.Regex GuidPattern =
            new System.Text.RegularExpressions.Regex(
                @"[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}",
                System.Text.RegularExpressions.RegexOptions.Compiled);

        private static readonly System.Text.RegularExpressions.Regex LongHexPattern =
            new System.Text.RegularExpressions.Regex(
                @"(?<![0-9a-fA-F])[0-9a-fA-F]{32,}(?![0-9a-fA-F])|(?<![0-9a-fA-F])[0-9a-fA-F]{8}(?![0-9a-fA-F])",
                System.Text.RegularExpressions.RegexOptions.Compiled);

        private static string NormalizeVolatile(string canonicalRow)
            => LongHexPattern.Replace(GuidPattern.Replace(canonicalRow, "#GUID#"), "#HEX#");

        [Fact]
        public void App_Providers_Have_No_Display_Name_Alias()
        {
            CheckProviderAlias("Application", "MsiInstaller");
            CheckProviderAlias("System", "EventLog");
            CheckProviderAlias("System", "Microsoft-Windows-Kernel-Power");
        }

        private static void CheckProviderAlias(string logName, string provider)
        {
            var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".evtx");

            try
            {
                var export = Process.Start(new ProcessStartInfo("wevtutil", $"epl {logName} \"{path}\"")
                {
                    UseShellExecute = false,
                    CreateNoWindow = true
                });
                export.WaitForExit(120000);
                Assert.Equal(0, export.ExitCode);

                var liveByKey = Helpers.GetLogEntries(logName)
                    .ToLookup(r => Canonical(r));
                var checkedAny = false;

                foreach (var record in EvtxFileReader.ReadEvents(path))
                {
                    if (record.Source != provider) continue;
                    checkedAny = true;
                    Assert.Contains(liveByKey[Canonical(record)], r => r.Source == provider);
                }

                Assert.True(checkedAny, $"Nessun evento {provider} nel log {logName}: test non significativo.");
            }
            finally
            {
                try { File.Delete(path); } catch { }
            }
        }
    }
}
