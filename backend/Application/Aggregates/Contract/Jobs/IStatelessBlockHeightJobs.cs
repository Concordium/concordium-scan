using System.Threading;
using System.Threading.Tasks;

namespace Application.Aggregates.Contract.Jobs;

/// <summary>
/// This interface is used by jobs which should be able to run in parallel
/// without sharing state between block height imports.
/// </summary>
interface IStatelessBlockHeightJobs
{
    /// <summary>
    /// This returns a unique identifier of the job.
    ///
    /// WARNING: changing this could result in already executed jobs rerunning.
    /// </summary>
    string GetUniqueIdentifier();

    /// <summary>
    /// Returns the maximum height which should be processed. When used in a batch processed the final height
    /// processed since a import job may increment by a batch size.
    /// 
    /// This shouldn't necessary be static and could change over time and the data source the
    /// job is using is updating.
    /// </summary>
    Task<long> GetMaximumHeight(CancellationToken token);

    /// <summary>
    /// Opportunity for the job to set a metric related to the jobs current processing state.
    ///
    /// Called every <see cref="Application.Aggregates.Contract.Configurations.ContractAggregateOptions.MetricDelay"/>.
    /// </summary>
    Task UpdateMetric(CancellationToken token);

    /// <summary>
    /// Batch process which should be executed between the input heights.
    ///
    /// Both <see cref="heightFrom"/> and <see cref="heightTo"/> should be inclusive.
    ///
    /// The job should return the number of affected heights. If all heights in batch are processed this would equal
    /// <see cref="heightTo"/> - <see cref="heightFrom"/> + 1.
    /// In the process the process validates a height hasn't been processed before the return value
    /// would be lower.
    /// </summary>
    Task<ulong> BatchImportJob(ulong heightFrom, ulong heightTo, CancellationToken token = default);
}