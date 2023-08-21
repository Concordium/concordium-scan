namespace Application.Aggregates.SmartContract.Entities;

/// <summary>
/// Stored when a block height has been processed by <see cref="SmartContractAggregate"/>.
/// </summary>
public sealed class SmartContractReadHeight
{
    public long Id { get; init; }
    public ulong BlockHeight { get; init; }
    public ImportSource Source { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    
    /// <summary>
    /// Needed for EF Core
    /// </summary>
#pragma warning disable CS8618
    private SmartContractReadHeight()
#pragma warning restore CS8618
    {}
    
    public SmartContractReadHeight(ulong blockHeight, ImportSource source)
    {
        BlockHeight = blockHeight;
        Source = source;
    }
}