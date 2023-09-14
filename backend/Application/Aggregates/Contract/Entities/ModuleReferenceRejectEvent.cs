using Application.Aggregates.Contract.Types;
using Application.Api.GraphQL.Accounts;
using Application.Api.GraphQL.Transactions;

namespace Application.Aggregates.Contract.Entities;

/// <summary>
/// Event which is generated when a module reference is created.
/// </summary>
public sealed class ModuleReferenceRejectEvent
{
    public ulong BlockHeight { get; init; }
    public string TransactionHash { get; init; } = null!;
    public ulong TransactionIndex { get; init; }
    public string ModuleReference { get; init; } = null!;
    public AccountAddress Sender { get; init; } = null!;
    public TransactionRejectReason RejectedEvent { get; init; } = null!;
    public ImportSource Source { get; init; }
    public DateTimeOffset BlockSlotTime { get; init; }
    public DateTimeOffset CreatedAt { get; init; } = DateTime.UtcNow;
    
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
        DateTimeOffset blockSlotTime
    )
    {
        BlockHeight = blockHeight;
        TransactionHash = transactionHash;
        TransactionIndex = transactionIndex;
        ModuleReference = moduleReference;
        Sender = sender;
        RejectedEvent = rejectedEvent;
        Source = source;
        BlockSlotTime = blockSlotTime;
    }
}