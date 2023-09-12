using Application.Aggregates.Contract.Types;
using Application.Api.GraphQL;

namespace Application.Aggregates.Contract.Entities;

/// <summary>
/// This class contain base identification which is used across multiple
/// contract events.
/// </summary>
public abstract class BaseIdentification
{
    public ulong BlockHeight { get; protected init; }
    public string TransactionHash { get; protected init; } = null!;
    public ulong TransactionIndex { get; protected init; }
    public ulong ContractAddressIndex { get; protected init; }
    public ulong ContractAddressSubIndex { get; protected init; }
    public ImportSource Source { get; protected init; }
    public DateTimeOffset BlockSlotTime { get; protected init; }
    public DateTimeOffset CreatedAt { get; init; } = DateTime.UtcNow;
    
    /// <summary>
    /// Needed for EF Core
    /// </summary>
    protected BaseIdentification()
    {}
    
    internal BaseIdentification(
        ulong blockHeight,
        string transactionHash,
        ulong transactionIndex,
        ContractAddress contractAddress,
        ImportSource source,
        DateTimeOffset blockSlotTime)
    {
        BlockHeight = blockHeight;
        TransactionHash = transactionHash;
        TransactionIndex = transactionIndex;
        ContractAddressIndex = contractAddress.Index;
        ContractAddressSubIndex = contractAddress.SubIndex;
        Source = source;
        BlockSlotTime = blockSlotTime;
    }
    
}