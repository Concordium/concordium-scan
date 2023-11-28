using System.Threading;
using System.Threading.Tasks;
using Application.Jobs;

namespace Application.Database.MigrationJobs;

/// <summary>
/// Interfaces which should be used for all jobs relevant for
/// main import flow.
/// </summary>
public interface IMainMigrationJob : IJob
{
    Task StartImport(CancellationToken token);

    /// <summary>
    /// Returns if import from node should await job execution. 
    /// </summary>
    bool ShouldNodeImportAwait();
}
