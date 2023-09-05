using System.Threading;
using System.Threading.Tasks;

namespace Application.Aggregates.Contract.Jobs;

/// <summary>
/// Interfaces which should be used for all jobs relevant for
/// Smart Contracts. 
/// </summary>
public interface IContractJob
{
    Task StartImport(CancellationToken token);
    /// <summary>
    /// This returns a unique identifier of the job.
    ///
    /// WARNING: changing this could result in already executed jobs rerunning.
    /// </summary>
    string GetUniqueIdentifier();
}