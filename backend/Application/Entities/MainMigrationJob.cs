using Application.Jobs;

namespace Application.Entities;

/// <summary>
/// Jobs related to migration jobs of main process, which has successfully executed.
/// </summary>
public sealed class MainMigrationJob : IJobEntity<MainMigrationJob>
{
    public string Job { get; init; } = null!;
    public DateTimeOffset CreatedAt { get; } = DateTime.UtcNow;
    
    public MainMigrationJob()
    {}

    public MainMigrationJob(string job)
    {
        Job = job;
    }
}
