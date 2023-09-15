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
    ///
    /// Defaults to `-1`w which is retry forever.
    /// </summary>
    public int RetryCount { get; set; } = -1;
    /// <summary>
    /// Time between retries in retry policies.
    /// </summary>
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(3);
    /// <summary>
    /// Delay between updating metrics which relies on fetch.
    /// </summary>
    public TimeSpan MetricDelay { get; set; } = TimeSpan.FromSeconds(5);
}
