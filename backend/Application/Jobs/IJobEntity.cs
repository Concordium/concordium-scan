namespace Application.Jobs;

/// <summary>
/// Base interface which job entities should inherit from.
/// </summary>
internal interface IJobEntity<T>
{
    /// <summary>
    /// Identifier of job.
    /// </summary>
    public string Job { get; init; }
}
