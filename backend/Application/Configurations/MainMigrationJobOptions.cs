namespace Application.Configurations;

public sealed class MainMigrationJobOptions
{
    /// <summary>
    /// Set options for jobs related migration jobs for main import flow.
    ///
    /// Done as dictionary such that it can be changed from configurations. Key is unique identifier of job and
    /// it defined within the jobs class.
    /// </summary>
    public IDictionary<string, JobOptions> Jobs { get; set; } =
        new Dictionary<string, JobOptions>();
    /// <summary>
    /// Delay which is used by the node importer between validation if all jobs has succeeded.
    /// </summary>
    public TimeSpan JobDelay { get; set; } = TimeSpan.FromSeconds(10);
}
