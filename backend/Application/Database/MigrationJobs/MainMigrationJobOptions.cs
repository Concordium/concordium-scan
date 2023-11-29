using Application.Configurations;

namespace Application.Database.MigrationJobs;

public sealed class MainMigrationJobOptions
{
    /// <summary>
    /// Set options for jobs related to main process migrations.
    ///
    /// Done as dictionary such that it can be changed from configurations. Key is unique identifier of job and
    /// it defined within the jobs class.
    /// </summary>
    public IDictionary<string, JobOptions> Jobs { get; set; } =
        new Dictionary<string, JobOptions>();
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
}
