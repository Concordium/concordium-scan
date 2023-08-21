namespace Application.Aggregates.SmartContract.Entities;

/// <summary>
/// Jobs related to smart contracts, which has successfully executed.
/// </summary>
public sealed class SmartContractJob
{
    public string Job { get; set; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    
    /// <summary>
    /// Needed for EF Core
    /// </summary>
#pragma warning disable CS8618
    private SmartContractJob()
#pragma warning restore CS8618
    {}

    public SmartContractJob(string job)
    {
        Job = job;
    }
}