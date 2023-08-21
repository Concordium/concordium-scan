using Application.Aggregates.SmartContract.Jobs;

namespace Application.Aggregates.SmartContract.Configurations;

public class SmartContractAggregateOptions
{
    /// <summary>
    /// Set options for jobs related to smart contracts.
    ///
    /// Done as dictionary such that it can be changed from configurations. Key is unique identifier of job and
    /// it defined within the jobs class.
    /// </summary>
    public IDictionary<string, SmartContractAggregateJobOptions> Jobs { get; set; }
}