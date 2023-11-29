using System.Threading;
using System.Threading.Tasks;

namespace Application.Jobs;

public interface IJob
{
    Task StartImport(CancellationToken token);
    /// <summary>
    /// This returns a unique identifier of the job.
    ///
    /// WARNING: changing this could result in already executed jobs rerunning.
    /// </summary>
    string GetUniqueIdentifier();
    /// <summary>
    /// Returns if import from node should await job execution. 
    /// </summary>
    bool ShouldNodeImportAwait();
}
