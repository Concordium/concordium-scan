using System.Threading;
using System.Threading.Tasks;

namespace Application.Aggregates.Contract.Jobs;

interface IContractJobRepository
{
    /// <summary>
    /// Checks in storage if a job has been executed.
    ///
    /// If the job identifier is in the storage this function is expected to return true.
    /// Hence only successfully executed jobs should be placed in storage.
    /// </summary>
    Task<bool> DoesExistingJobExist(IContractJob job, CancellationToken token = default);

    /// <summary>
    /// Saves identifier of successfully executed job to storage.
    /// </summary>
    Task SaveSuccessfullyExecutedJob(IContractJob job, CancellationToken token = default);
}