using Application.Aggregates.Contract.Types;
using Application.Api.GraphQL;
using Application.Api.GraphQL.Accounts;
using Application.Api.GraphQL.Transactions;
using HotChocolate;

namespace Application.Aggregates.Contract.Entities;

/// <summary>
/// Event which is generated at smart contract interactions or updates.
/// </summary>
public sealed class ContractEvent : BaseIdentification
{
    public ulong ContractAddressIndex { get; init; }
    public ulong ContractAddressSubIndex { get; init; }
    [GraphQLIgnore]
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
    
    internal const string ContractEventsSql = @"
    SELECT 
        g0.block_height as BlockHeight,
        g0.transaction_index as TransactionIndex,
        g0.event_index as EventIndex,
        g0.contract_address_index as ContractAddressIndex,
        g0.contract_address_subindex as ContractAddressSubIndex,
        g0.block_slot_time as BlockSlotTime,
        g0.created_at as CreatedAt,
        g0.event as Event,
        g0.sender as Creator,
        g0.source as Source,
        g0.transaction_hash as TransactionHash
    FROM graphql_contract_events AS g0
    WHERE (g0.contract_address_index = @Index) AND (g0.contract_address_subindex = @Subindex)
    ORDER BY g0.block_height DESC, g0.transaction_index DESC, g0.event_index DESC;
";    
}
