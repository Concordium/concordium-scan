namespace Application.Aggregates.SmartContract.Configurations;

public class SmartContractAggregateJobOptions
{
    /// <summary>
    /// Whenever the difference between `max_imported_block_height` from table  `graphql_import_state` and
    /// jobs imported height is equal to or lower than this limit, the import stops.
    ///
    /// Having a high number would make the job stop earlier and the import from node instead starts.
    /// </summary>
    public uint Limit { get; set; } = 25;

    /// <summary>
    /// Number of tasks which should be used for parallelism. 
    /// </summary>
    public int NumberOfTask { get; set; } = 50;
}