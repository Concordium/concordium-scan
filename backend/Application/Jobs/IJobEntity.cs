namespace Application.Jobs;

/// <summary>
/// Base interface which job entities should inherit from.
/// </summary>
public interface IJobEntity
{
    /// <summary>
    /// Identifier of job.
    /// </summary>
    public string Job { get; init; }
}
