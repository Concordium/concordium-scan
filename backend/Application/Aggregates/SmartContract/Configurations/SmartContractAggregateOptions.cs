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
    /// <summary>
    /// Delay which is used by the node importer between validation if all jobs has succeeded.
    /// </summary>
    public TimeSpan JobDelay { get; set; } = TimeSpan.FromSeconds(10);
    /// <summary>
    /// Delay between retries in resilience policies.
    /// </summary>
    public TimeSpan DelayBetweenRetries { get; set; } = TimeSpan.FromSeconds(3);
    /// <summary>
    /// Number of times to retry.
    /// </summary>
    public uint RetryCount { get; set; } = 5;
}