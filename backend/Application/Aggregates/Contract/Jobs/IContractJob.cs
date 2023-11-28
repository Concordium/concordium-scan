using System.Threading;
using System.Threading.Tasks;
using Application.Jobs;

namespace Application.Aggregates.Contract.Jobs;

/// <summary>
/// Interfaces which should be used for all jobs relevant for
/// Smart Contracts. 
/// </summary>
public interface IContractJob : IJob
{
    Task StartImport(CancellationToken token);
    /// <summary>
    /// Returns if import from node should await job execution. 
    /// </summary>
    bool ShouldNodeImportAwait();
}
