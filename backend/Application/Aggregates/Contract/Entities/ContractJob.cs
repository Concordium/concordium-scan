using Application.Jobs;

namespace Application.Aggregates.Contract.Entities;

/// <summary>
/// Jobs related to smart contracts, which has successfully executed.
/// </summary>
public sealed class ContractJob : IJobEntity
{
    public string Job { get; init; } = null!;

    public DateTimeOffset CreatedAt { get; } = DateTime.UtcNow;
    
    public ContractJob()
    {}

    public ContractJob(string job)
    {
        Job = job;
    }
}
