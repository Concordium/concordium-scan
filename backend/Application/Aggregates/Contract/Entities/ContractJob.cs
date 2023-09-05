namespace Application.Aggregates.Contract.Entities;

/// <summary>
/// Jobs related to smart contracts, which has successfully executed.
/// </summary>
public sealed class ContractJob
{
    public string Job { get; set; }
    public DateTimeOffset CreatedAt { get; init; } = DateTime.UtcNow;
    
    /// <summary>
    /// Needed for EF Core
    /// </summary>
#pragma warning disable CS8618
    private ContractJob()
#pragma warning restore CS8618
    {}

    public ContractJob(string job)
    {
        Job = job;
    }
}