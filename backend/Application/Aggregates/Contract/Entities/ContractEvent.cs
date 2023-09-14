using Application.Aggregates.Contract.Types;
using Application.Api.GraphQL;
using Application.Api.GraphQL.Accounts;
using Application.Api.GraphQL.Transactions;

namespace Application.Aggregates.Contract.Entities;

/// <summary>
/// Event which is generated at smart contract interactions or updates.
/// </summary>
public sealed class ContractEvent : BaseIdentification
{
    public ulong ContractAddressIndex { get; init; }
    public ulong ContractAddressSubIndex { get; init; }
    public uint EventIndex { get; init; }
    public AccountAddress Sender { get; init; } = null!;
    public TransactionResultEvent Event { get; init; } = null!;

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
        DateTimeOffset blockSlotTime) :
        base(blockHeight, transactionHash, transactionIndex, source, blockSlotTime)
    {
        ContractAddressIndex = contractAddress.Index;
        ContractAddressSubIndex = contractAddress.SubIndex;
        EventIndex = eventIndex;
        Sender = sender;
        Event = @event;
    }
}
