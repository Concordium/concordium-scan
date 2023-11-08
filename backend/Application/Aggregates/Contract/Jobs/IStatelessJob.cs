using System.Threading;
using System.Threading.Tasks;

namespace Application.Aggregates.Contract.Jobs;

/// <summary>
/// This interface is used by jobs which should be able to run in parallel
/// without sharing state between processes.
/// </summary>
public interface IStatelessJob
{
    /// <summary>
    /// This returns a unique identifier of the job.
    ///
    /// WARNING: changing this could result in already executed jobs rerunning.
    /// </summary>
    string GetUniqueIdentifier();

    /// <summary>
    /// Get an enumerable with identifiers which can be used by parallel processes to identify
    /// what work needs to be done.
    /// </summary>
    Task<IEnumerable<int>> GetIdentifierSequence(CancellationToken cancellationToken);

    /// <summary>
    /// Process identifier.
    /// </summary>
    ValueTask Process(int identifier, CancellationToken token = default);
    
    /// <summary>
    /// Returns if import from node should await job execution. 
    /// </summary>
    bool ShouldNodeImportAwait();
}
