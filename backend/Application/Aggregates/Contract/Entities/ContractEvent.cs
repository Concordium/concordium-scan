using System.Threading.Tasks;
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
    public TransactionResultEvent Event { get; private set; } = null!;
    [GraphQLIgnore] 
    public DateTimeOffset? UpdatedAt { get; private set; }

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

    /// <summary>
    /// Try parse hexadecimal events and parameters in <see cref="Event"/>. If parsing succeeds the event is overriden.
    /// </summary>
    internal async Task ParseEvent(IContractRepository contractRepository, IModuleReadonlyRepository moduleReadonlyRepository)
    {
        var updated = Event switch
        {
            ContractCall contractCall => await contractCall.TryUpdate(
                    moduleReadonlyRepository,
                    BlockHeight,
                    TransactionIndex,
                    EventIndex
                ),
            ContractInitialized contractInitialized => await contractInitialized.TryUpdateWithParsedEvents(moduleReadonlyRepository),
            ContractInterrupted contractInterrupted => await contractInterrupted.TryUpdateWithParsedEvents(
                    contractRepository,
                    moduleReadonlyRepository,
                    BlockHeight,
                    TransactionIndex,
                    EventIndex),
            ContractUpdated contractUpdated => await contractUpdated.TryUpdate(
                    moduleReadonlyRepository,
                    BlockHeight,
                    TransactionIndex,
                    EventIndex
                ),
            _ => Event
        };
        if (updated == null)
        {
            return;
        }
        Event = updated;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Check if hexadecimal fields in <see cref="ContractEvent"/>'s has been parsed or there is nothing to parse.
    /// </summary>
    internal bool IsHexadecimalFieldsParsed()
    {
        return Event switch
        {
            ContractCall contractCall => contractCall.ContractUpdated.Message != null &&
                                         contractCall.ContractUpdated.Events != null,
            ContractInitialized contractInitialized => contractInitialized.Events != null,
            ContractInterrupted contractInterrupted => contractInterrupted.Events != null,
            ContractUpdated contractUpdated => contractUpdated.Message != null && contractUpdated.Events != null,
            _ => true
        };
    }
    
    /// <summary>
    /// Returns contract initialized event(s) for contracts. There should always be only one.
    ///
    /// <see cref="Application.Api.GraphQL.EfCore.Converters.Json.TransactionResultEventConverter"/> has event mapping.
    /// </summary>
    internal const string ContractInitializedEventSql = @"
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
WHERE (g0.contract_address_index = @Index) AND (g0.contract_address_subindex = @Subindex) AND
g0.event ->> 'tag' = '16' 
";
}
