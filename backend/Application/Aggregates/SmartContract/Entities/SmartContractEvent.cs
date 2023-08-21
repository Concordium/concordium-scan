using Application.Aggregates.SmartContract.Types;
using Application.Api.GraphQL;
using Application.Api.GraphQL.Transactions;

namespace Application.Aggregates.SmartContract.Entities;

/// <summary>
/// Event which is generated at smart contract interactions or updates.
/// </summary>
public sealed class SmartContractEvent
{
    public ulong BlockHeight { get; init; }
    public string TransactionHash { get; init; }
    public ulong TransactionIndex { get; init; }
    public uint EventIndex { get; init; }
    public ulong ContractAddressIndex { get; init; }
    public ulong ContractAddressSubIndex { get; init; }
    public TransactionResultEvent Event { get; init; }
    public ImportSource Source { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Needed for EF Core
    /// </summary>
#pragma warning disable CS8618
    private SmartContractEvent()
#pragma warning restore CS8618
    {}

    internal SmartContractEvent(
        ulong blockHeight,
        string transactionHash,
        ulong transactionIndex,
        uint eventIndex,
        ContractAddress contractAddress,
        TransactionResultEvent @event,
        ImportSource source)
    {
        BlockHeight = blockHeight;
        TransactionHash = transactionHash;
        TransactionIndex = transactionIndex;
        EventIndex = eventIndex;
        ContractAddressIndex = contractAddress.Index;
        ContractAddressSubIndex = contractAddress.SubIndex;
        Event = @event;
        Source = source;
    }
}