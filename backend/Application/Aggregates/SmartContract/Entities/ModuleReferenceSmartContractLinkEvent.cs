using Application.Aggregates.SmartContract.Types;
using Application.Api.GraphQL;

namespace Application.Aggregates.SmartContract.Entities;

/// <summary>
/// Event which is generated when a module reference and smart contract is linked.
///
/// This is either at contract initialization or contract upgrade.
/// </summary>
public sealed class ModuleReferenceSmartContractLinkEvent
{
    public ulong BlockHeight { get; init; }
    public string TransactionHash { get; init; }
    public ulong TransactionIndex { get; init; }
    public uint EventIndex { get; init; }
    public string ModuleReference { get; init; }
    public ulong ContractAddressIndex { get; init; }
    public ulong ContractAddressSubIndex { get; init; }
    public ImportSource Source { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    
    /// <summary>
    /// Needed for EF Core
    /// </summary>
#pragma warning disable CS8618
    private ModuleReferenceSmartContractLinkEvent()
#pragma warning restore CS8618
    {}

    public ModuleReferenceSmartContractLinkEvent(
        ulong blockHeight,
        string transactionHash,
        ulong transactionIndex,
        uint eventIndex,
        string moduleReference,
        ContractAddress contractAddress,
        ImportSource source
    )
    {
        BlockHeight = blockHeight;
        TransactionHash = transactionHash;
        TransactionIndex = transactionIndex;
        EventIndex = eventIndex;
        ModuleReference = moduleReference;
        ContractAddressIndex = contractAddress.Index;
        ContractAddressSubIndex = contractAddress.SubIndex;
        Source = source;
    }
}