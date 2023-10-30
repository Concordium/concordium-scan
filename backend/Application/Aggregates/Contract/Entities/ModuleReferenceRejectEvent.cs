using Application.Aggregates.Contract.Types;
using Application.Api.GraphQL.Accounts;
using Application.Api.GraphQL.Transactions;

namespace Application.Aggregates.Contract.Entities;

/// <summary>
/// Event which is generated when a module reference is created.
/// </summary>
public sealed class ModuleReferenceRejectEvent : BaseIdentification
{
    public string ModuleReference { get; init; } = null!;
    public AccountAddress Sender { get; init; } = null!;
    public TransactionRejectReason RejectedEvent { get; init; } = null!;
    
    /// <summary>
    /// Needed for EF Core
    /// </summary>
    private ModuleReferenceRejectEvent()
    {}

    public ModuleReferenceRejectEvent(
        ulong blockHeight,
        string transactionHash,
        ulong transactionIndex,
        string moduleReference,
        AccountAddress sender,
        TransactionRejectReason rejectedEvent,
        ImportSource source,
        DateTimeOffset blockSlotTime) : 
        base(blockHeight, transactionHash, transactionIndex, source, blockSlotTime)
    {
        ModuleReference = moduleReference;
        Sender = sender;
        RejectedEvent = rejectedEvent;
    }
    
    internal const string ModuleReferenceRejectEventsSql = @"
SELECT 
    g0.block_height as BlockHeight, 
    g0.transaction_index as TransactionIndex,
    g0.module_reference as ModuleReference, 
    g0.block_slot_time as BlockSlotTime, 
    g0.created_at as CreatedAt, 
    g0.reject_event as RejectedEvent, 
    g0.sender as Sender, 
    g0.source as Source, 
    g0.transaction_hash as TransactionHash
    FROM graphql_module_reference_reject_events AS g0
    WHERE g0.module_reference = @ModuleReference
    ORDER BY g0.block_height DESC, g0.transaction_index DESC
";
}
