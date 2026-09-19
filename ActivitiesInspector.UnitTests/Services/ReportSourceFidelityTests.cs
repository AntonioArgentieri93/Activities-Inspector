using Activities_Inspector.Messages;
using Activities_Inspector.Models;
using Activities_Inspector.Services;
using Activities_Inspector.Services.Evidence;
using Activities_Inspector.Services.Reporting;
using Activities_Inspector.ViewModels;
using ActivitiesInspector.UnitTests.Doubles;
using GalaSoft.MvvmLight.Messaging;
using MigraDocCore.DocumentObjectModel;
using MigraDocCore.DocumentObjectModel.Tables;
using System;
using System.Linq;
using Xunit;

namespace ActivitiesInspector.UnitTests.Services
{
    public class ReportSourceFidelityTests
    {
        [Fact]
        public void Mixed_Sources_Cover_Shows_Mista()
        {
            var audit = new FakeAuditTrail();
            var sources = new EvidenceSourceProvider();
            var vm = new ReportViewModel(new FakeReportService(), new TestDialogService(), new FakeWindowFactory(), Messenger.Default, audit, sources);

            // Simulate two searches with different sources
            Messenger.Default.Send(new OnUsageInfosChangedMessage(
                new System.Collections.Generic.List<UsageInfo> { new UsageInfo(new IntervalEntry(DateTime.Now, DateTime.Now.AddHours(1)), TimeSpan.FromHours(1), "PC") },
                manifest: new System.Collections.Generic.List<IntegrityRecord>(),
                source: "Sistema live"));
            Messenger.Default.Send(new OnPrefetchInfoEntriesChangedMessage(
                new System.Collections.Generic.List<PrefetchInfoEntry> { new PrefetchInfoEntry("A.EXE", "A.pf", DateTime.Now, ".EXE") },
                manifest: new System.Collections.Generic.List<IntegrityRecord>(),
                source: "Immagine: E:\\Caso"));

            // Use reflection to call private GetEvidenceSourceForReport via GenerateReport path: instead test the logic directly
            var method = typeof(ReportViewModel).GetMethod("GetEvidenceSourceForReport", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var result = (string)method.Invoke(vm, null);

            Assert.Contains("Mista", result);
            Assert.Contains("Sistema live", result);
            Assert.Contains("Immagine", result);
        }

        [Fact]
        public void Single_Source_Cover_Shows_That_Source()
        {
            var audit = new FakeAuditTrail();
            var sources = new EvidenceSourceProvider();
            var vm = new ReportViewModel(new FakeReportService(), new TestDialogService(), new FakeWindowFactory(), Messenger.Default, audit, sources);

            Messenger.Default.Send(new OnUsageInfosChangedMessage(
                new System.Collections.Generic.List<UsageInfo> { new UsageInfo(new IntervalEntry(DateTime.Now, DateTime.Now.AddHours(1)), TimeSpan.FromHours(1), "PC") },
                source: "Immagine: E:\\Caso"));

            var method = typeof(ReportViewModel).GetMethod("GetEvidenceSourceForReport", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var result = (string)method.Invoke(vm, null);

            Assert.Equal("Immagine: E:\\Caso", result);
        }

        [Fact]
        public void Per_Section_Source_Note_Appears_In_Report()
        {
            var doc = new Document();
            var section = doc.AddSection();
            var info = new UsageInfo(new IntervalEntry(new DateTime(2024, 1, 1, 8, 0, 0), new DateTime(2024, 1, 1, 9, 0, 0)), TimeSpan.FromHours(1), "PC");
            ReportTablesBuilder.AddUsageInfos(new[] { info }, section, source: "Immagine: E:\\Caso");

            var texts = section.Elements.OfType<Paragraph>().Select(p => string.Concat(p.Elements.OfType<Text>().Select(t => t.Content))).ToList();
            Assert.Contains(texts, t => t.Contains("Sorgente: Immagine"));
        }
    }
}
