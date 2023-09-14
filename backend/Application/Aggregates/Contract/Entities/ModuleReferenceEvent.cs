using Application.Aggregates.Contract.Types;
using Application.Api.GraphQL.Accounts;

namespace Application.Aggregates.Contract.Entities;

/// <summary>
/// Event which is generated when a module reference is created.
/// </summary>
public sealed class ModuleReferenceEvent : BaseIdentification
{
    public uint EventIndex { get; init; }
    public string ModuleReference { get; init; } = null!;
    public AccountAddress Sender { get; init; } = null!;
    
    /// <summary>
    /// Needed for EF Core
    /// </summary>
    private ModuleReferenceEvent()
    {}

    public ModuleReferenceEvent(
        ulong blockHeight,
        string transactionHash,
        ulong transactionIndex,
        uint eventIndex,
        string moduleReference,
        AccountAddress sender,
        ImportSource source,
        DateTimeOffset blockSlotTime) : 
        base(blockHeight, transactionHash, transactionIndex, source, blockSlotTime)
    {
        EventIndex = eventIndex;
        ModuleReference = moduleReference;
        Sender = sender;
    }
}
