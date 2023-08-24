namespace Application.Aggregates.SmartContract.Configurations;

public class SmartContractAggregateJobOptions
{
    /// <summary>
    /// Number of tasks which should be used for parallelism. 
    /// </summary>
    public int NumberOfTask { get; set; } = 5;
    /// <summary>
    /// Each task when processing will load multiple blocks and transaction to avoid databases round trips.
    ///
    /// Increasing batch size will increase memory consumption on job since more will be loaded into memory.
    /// </summary>
    public int BatchSize { get; set; } = 10_000;
}