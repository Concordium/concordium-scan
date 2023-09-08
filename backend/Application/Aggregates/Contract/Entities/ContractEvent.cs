using Application.Aggregates.Contract.Types;
using Application.Api.GraphQL;
using Application.Api.GraphQL.Accounts;
using Application.Api.GraphQL.Transactions;

namespace Application.Aggregates.Contract.Entities;

/// <summary>
/// Event which is generated at smart contract interactions or updates.
/// </summary>
public sealed class ContractEvent
{
    public ulong BlockHeight { get; init; }
    public string TransactionHash { get; init; } = null!;
    public ulong TransactionIndex { get; init; }
    public uint EventIndex { get; init; }
    public ulong ContractAddressIndex { get; init; }
    public ulong ContractAddressSubIndex { get; init; }
    public AccountAddress Sender { get; init; } = null!;
    public TransactionResultEvent Event { get; init; } = null!;
    public ImportSource Source { get; init; }
    public DateTimeOffset BlockSlotTime { get; init; }
    public DateTimeOffset CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Needed for EF Core
    /// </summary>
    private ContractEvent()
    {}

    internal ContractEvent(
        ulong blockHeight,
        string transactionHash,
        ulong transactionIndex,
        uint eventIndex,
        ContractAddress contractAddress,
        AccountAddress sender,
        TransactionResultEvent @event,
        ImportSource source,
        DateTimeOffset blockSlotTime)
    {
        BlockHeight = blockHeight;
        TransactionHash = transactionHash;
        TransactionIndex = transactionIndex;
        EventIndex = eventIndex;
        ContractAddressIndex = contractAddress.Index;
        ContractAddressSubIndex = contractAddress.SubIndex;
        Sender = sender;
        Event = @event;
        Source = source;
        BlockSlotTime = blockSlotTime;
    }
}