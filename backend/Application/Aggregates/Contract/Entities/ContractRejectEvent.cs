using Application.Aggregates.Contract.Types;
using Application.Api.GraphQL;
using Application.Api.GraphQL.Accounts;
using Application.Api.GraphQL.Transactions;

namespace Application.Aggregates.Contract.Entities;

/// <summary>
/// Event which is generated at rejected events on a contract instance.
/// </summary>
public sealed class ContractRejectEvent
{
    public ulong BlockHeight { get; init; }
    public string TransactionHash { get; init; } = null!;
    public ulong TransactionIndex { get; init; }
    public ulong ContractAddressIndex { get; init; }
    public ulong ContractAddressSubIndex { get; init; }
    public AccountAddress Sender { get; init; } = null!;
    public TransactionRejectReason RejectedEvent { get; init; } = null!;
    public ImportSource Source { get; init; }
    public DateTimeOffset BlockSlotTime { get; init; }
    public DateTimeOffset CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Needed for EF Core
    /// </summary>
    private ContractRejectEvent()
    {}

    internal ContractRejectEvent(
        ulong blockHeight,
        string transactionHash,
        ulong transactionIndex,
        ContractAddress contractAddress,
        AccountAddress sender,
        TransactionRejectReason rejectedEvent,
        ImportSource source,
        DateTimeOffset blockSlotTime)
    {
        BlockHeight = blockHeight;
        TransactionHash = transactionHash;
        TransactionIndex = transactionIndex;
        ContractAddressIndex = contractAddress.Index;
        ContractAddressSubIndex = contractAddress.SubIndex;
        Sender = sender;
        RejectedEvent = rejectedEvent;
        Source = source;
        BlockSlotTime = blockSlotTime;
    }
}