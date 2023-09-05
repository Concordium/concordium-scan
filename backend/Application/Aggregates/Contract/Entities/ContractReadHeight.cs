using Application.Aggregates.Contract.Types;

namespace Application.Aggregates.Contract.Entities;

/// <summary>
/// Stored when a block height has been processed by <see cref="ContractAggregate"/>.
/// </summary>
public sealed class ContractReadHeight
{
    public long Id { get; init; }
    public ulong BlockHeight { get; init; }
    public ImportSource Source { get; init; }
    public DateTimeOffset CreatedAt { get; init; } = DateTime.UtcNow;
    
    /// <summary>
    /// Needed for EF Core
    /// </summary>
    private ContractReadHeight()
    {}
    
    public ContractReadHeight(ulong blockHeight, ImportSource source)
    {
        BlockHeight = blockHeight;
        Source = source;
    }
}