using ActivitiesInspector.UnitTests.Doubles;
using Activities_Inspector.Models;
using Activities_Inspector.Services;
using Activities_Inspector.Services.Evidence;
using Activities_Inspector.ViewModels;
using CSharpFunctionalExtensions;
using GalaSoft.MvvmLight.Messaging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ActivitiesInspector.UnitTests.ViewModels
{
    public class TimeIntervalsViewModelTests
    {
        [Fact]
        public async Task Export_Is_Enabled_After_Successful_Load()
        {
            var dialogs = new TestDialogService();
            var audit = new FakeAuditTrail();
            var sources = new EvidenceSourceProvider();
            var vm = new TimeIntervalsViewModel(
                new FakeUsageLogTimeService(),
                dialogs,
                new FakeExporter(),
                Messenger.Default,
                audit,
                sources);

            Assert.False(vm.ExportCommand.CanExecute(null));

            // Come fa WPF: valuta CanExecute a ogni raise e tiene l'ultima valutazione.
            bool? lastEvaluated = null;
            vm.ExportCommand.CanExecuteChanged += (s, e) => lastEvaluated = vm.ExportCommand.CanExecute(null);

            vm.LoadIntervalsCommand.Execute(null);

            var sw = Stopwatch.StartNew();
            while ((vm.IsBusy || vm.Infos == null) && sw.Elapsed < TimeSpan.FromSeconds(5))
                await Task.Delay(50);

            Assert.False(vm.IsBusy);
            Assert.NotNull(vm.Infos);
            Assert.NotEmpty(vm.Infos);
            Assert.Empty(dialogs.Errors);
            Assert.True(lastEvaluated);
            Assert.True(vm.ExportCommand.CanExecute(null));
            Assert.Contains(audit.Records, r => r.Category == AuditCategory.Ricerca);
        }

        private sealed class FakeUsageLogTimeService : IUsageLogTimeService
        {
            public IReadOnlyList<IntegrityRecord> LastIntegrityManifest { get; } = new List<IntegrityRecord>();

            public Task<Result<List<IEventRecord>>> GetSystemEventsAsync(CancellationToken cancellationToken = default)
                => Task.FromResult(Result.Success(new List<IEventRecord>()));

            public IEnumerable<UsageInfo> BuildUsageInfo(IEnumerable<IEventRecord> events)
                => new[]
                {
                    new UsageInfo(
                        new IntervalEntry(new DateTime(2024, 1, 15, 8, 0, 0), new DateTime(2024, 1, 15, 18, 0, 0)),
                        TimeSpan.FromHours(10), "PC")
                };
        }

        private sealed class FakeExporter : IEntriesExporter
        {
            public Task<Result<string>> SaveEntriesDataAsync(IEnumerable<Entry> entries, EntryType entryType, CancellationToken cancellationToken = default, string footerNote = null)
                => Task.FromResult(Result.Success("Output"));
        }
    }
}
