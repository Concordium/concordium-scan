namespace Application.Aggregates.SmartContract.Entities;

/// <summary>
/// Event which is generated when a module reference is created.
/// </summary>
public sealed class ModuleReferenceEvent
{
    public ulong BlockHeight { get; init; }
    public string TransactionHash { get; init; }
    public ulong TransactionIndex { get; init; }
    public uint EventIndex { get; init; }
    public string ModuleReference { get; init; }
    public ImportSource Source { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    
    /// <summary>
    /// Needed for EF Core
    /// </summary>
#pragma warning disable CS8618
    private ModuleReferenceEvent()
#pragma warning restore CS8618
    {}

    public ModuleReferenceEvent(
        ulong blockHeight,
        string transactionHash,
        ulong transactionIndex,
        uint eventIndex,
        string moduleReference,
        ImportSource source
    )
    {
        BlockHeight = blockHeight;
        TransactionHash = transactionHash;
        TransactionIndex = transactionIndex;
        EventIndex = eventIndex;
        ModuleReference = moduleReference;
        Source = source;
    }
}