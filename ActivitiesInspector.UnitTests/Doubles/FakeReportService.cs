using Activities_Inspector.Models;
using Activities_Inspector.Services;
using CSharpFunctionalExtensions;
using System.Threading;
using System.Threading.Tasks;

namespace ActivitiesInspector.UnitTests.Doubles
{
    public sealed class FakeReportService : IReportService
    {
        public int Calls { get; private set; }

        public Task<Result> CreatePdfFileAsync(ReportContent content, CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.FromResult(Result.Success());
        }
    }
}
