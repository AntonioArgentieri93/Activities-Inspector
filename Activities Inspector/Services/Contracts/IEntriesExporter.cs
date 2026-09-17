using CSharpFunctionalExtensions;
using Activities_Inspector.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Activities_Inspector.Services
{
    public interface IEntriesExporter
    {
        Task<Result> SaveEntriesDataAsync(IEnumerable<Entry> entries, EntryType entryType, CancellationToken cancellationToken = default, string footerNote = null);
    }
}