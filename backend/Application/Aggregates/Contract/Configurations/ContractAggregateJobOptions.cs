namespace Application.Aggregates.Contract.Configurations;

public class ContractAggregateJobOptions
{
    /// <summary>
    /// Number of tasks which should be used for parallelism.
    ///
    /// By using default `-1` there isn't any limit and the runtime optimizes the worker counts.
    /// </summary>
    public int MaxParallelTasks { get; set; } = -1;
    /// <summary>
    /// Each task when processing will load multiple blocks and transaction to avoid databases round trips.
    ///
    /// Increasing batch size will increase memory consumption on job since more will be loaded into memory.
    /// </summary>
    public int BatchSize { get; set; } = 10_000;
}