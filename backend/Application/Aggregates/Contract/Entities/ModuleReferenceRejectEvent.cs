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
}
