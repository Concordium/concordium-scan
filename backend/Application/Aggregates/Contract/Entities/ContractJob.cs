namespace Application.Aggregates.Contract.Entities;

/// <summary>
/// Jobs related to smart contracts, which has successfully executed.
/// </summary>
public sealed class ContractJob
{
    public string Job { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; init; } = DateTime.UtcNow;
    
    /// <summary>
    /// Needed for EF Core
    /// </summary>
    private ContractJob()
    {}

    public ContractJob(string job)
    {
        Job = job;
    }
}
