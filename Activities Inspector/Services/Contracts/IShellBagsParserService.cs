using CSharpFunctionalExtensions;
using Activities_Inspector.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Activities_Inspector.Services
{
    public interface IShellBagsParserService
    {
        Task<Result<ShellBagsResult>> ParseShellBagsAsync(CancellationToken cancellationToken = default);
    }
}