namespace Application.Entities;

/// <summary>
/// Jobs related to migration jobs of main process, which has successfully executed.
/// </summary>
public sealed class MainMigrationJob
{
    public string Job { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; init; } = DateTime.UtcNow;
    
    /// <summary>
    /// Needed for EF Core
    /// </summary>
    private MainMigrationJob()
    {}

    public MainMigrationJob(string job)
    {
        Job = job;
    }
}
