using Application.Jobs;

namespace Application.Database.MigrationJobs;

/// <summary>
/// Interfaces which should be used for all jobs relevant for
/// main import flow.
/// </summary>
public interface IMainMigrationJob : IJob
{}
