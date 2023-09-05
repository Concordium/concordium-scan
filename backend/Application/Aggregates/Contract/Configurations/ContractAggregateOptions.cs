namespace Application.Aggregates.Contract.Configurations;

public class ContractAggregateOptions
{
    /// <summary>
    /// Set options for jobs related to contracts.
    ///
    /// Done as dictionary such that it can be changed from configurations. Key is unique identifier of job and
    /// it defined within the jobs class.
    /// </summary>
    public IDictionary<string, ContractAggregateJobOptions> Jobs { get; set; } =
        new Dictionary<string, ContractAggregateJobOptions>();
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