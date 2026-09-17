using Activities_Inspector.Models;
using CSharpFunctionalExtensions;
using System.Threading;
using System.Threading.Tasks;

namespace Activities_Inspector.Services
{
    public interface IReportService
    {
        Task<Result> CreatePdfFileAsync(ReportContent content, CancellationToken cancellationToken = default);
    }
}