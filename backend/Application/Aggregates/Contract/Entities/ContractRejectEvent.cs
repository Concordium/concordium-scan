using System.Threading.Tasks;
using Application.Aggregates.Contract.Types;
using Application.Api.GraphQL;
using Application.Api.GraphQL.Accounts;
using Application.Api.GraphQL.Transactions;
using HotChocolate;

namespace Application.Aggregates.Contract.Entities;

/// <summary>
/// Event which is generated at rejected events on a contract instance.
/// </summary>
public sealed class ContractRejectEvent : BaseIdentification
{
    public ulong ContractAddressIndex { get; init; }
    public ulong ContractAddressSubIndex { get; init; }
    public AccountAddress Sender { get; init; } = null!;
    public TransactionRejectReason RejectedEvent { get; private set; } = null!;
    [GraphQLIgnore] 
    public DateTimeOffset? UpdatedAt { get; private set; }
    
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
        DateTimeOffset blockSlotTime) :
        base(blockHeight, transactionHash, transactionIndex, source, blockSlotTime)
    {
        ContractAddressIndex = contractAddress.Index;
        ContractAddressSubIndex = contractAddress.SubIndex;
        Sender = sender;
        RejectedEvent = rejectedEvent;
    }
    
    /// <summary>
    /// Try parse hexadecimal events and parameters in <see cref="RejectedEvent"/> and override existing stored event with result.
    /// </summary>
    internal async Task ParseEvent(IModuleReadonlyRepository moduleReadonlyRepository)
    {
        var updated = RejectedEvent switch
        {
            RejectedReceive rejectedReceive => await rejectedReceive.TryUpdateMessage(
                moduleReadonlyRepository,
                BlockHeight,
                TransactionIndex),
            _ => RejectedEvent
        };
        if (updated == null)
        {
            return;
        }
        RejectedEvent = updated;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
    
    /// <summary>
    /// Check if <see cref="ContractRejectEvent"/> has been parsed.
    ///
    /// Also returns true if there is nothing to parse.
    /// </summary>
    internal bool IsParsed()
    {
        return RejectedEvent switch
        {
            RejectedReceive rejectedReceive => rejectedReceive.Message != null,
            _ => true
        };
    }
    
    internal const string ContractRejectEventsSql = @"
    SELECT 
        g0.block_height as BlockHeight,
        g0.transaction_index as TransactionIndex,
        g0.contract_address_index as ContractAddressIndex,
        g0.contract_address_subindex as ContractAddressSubIndex,
        g0.block_slot_time as BlockSlotTime,
        g0.created_at as CreatedAt,
        g0.reject_event as RejectedEvent,
        g0.sender as Sender, 
        g0.source as Source,
        g0.transaction_hash as TransactionHash
    FROM graphql_contract_reject_events AS g0
    WHERE (g0.contract_address_index = @Index) AND (g0.contract_address_subindex = @Subindex)
    ORDER BY g0.block_height DESC, g0.transaction_index DESC;
";
}
