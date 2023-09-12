using Application.Aggregates.Contract.Types;
using Application.Api.GraphQL;
using Application.Api.GraphQL.Accounts;

namespace Application.Aggregates.Contract.Entities;

/// <summary>
/// Event which is generated when a module reference and smart contract is linked.
///
/// This is either at contract initialization or contract upgrade.
/// </summary>
public sealed class ModuleReferenceContractLinkEvent : BaseIdentification
{
    public ulong ContractAddressIndex { get; init; }
    public ulong ContractAddressSubIndex { get; init; }
    public uint EventIndex { get; init; }
    public string ModuleReference { get; init; } = null!;
    public AccountAddress Sender { get; init; } = null!;
    public ModuleReferenceContractLinkAction LinkAction { get; init; }

    /// <summary>
    /// Needed for EF Core
    /// </summary>
    private ModuleReferenceContractLinkEvent()
    {}

    public ModuleReferenceContractLinkEvent(
        ulong blockHeight,
        string transactionHash,
        ulong transactionIndex,
        uint eventIndex,
        string moduleReference,
        ContractAddress contractAddress,
        AccountAddress sender,
        ImportSource source, 
        ModuleReferenceContractLinkAction linkAction,
        DateTimeOffset blockSlotTime
    ) :
        base(blockHeight, transactionHash, transactionIndex, source, blockSlotTime)
    {
        ContractAddressIndex = contractAddress.Index;
        ContractAddressSubIndex = contractAddress.SubIndex;
        EventIndex = eventIndex;
        ModuleReference = moduleReference;
        Sender = sender;
        LinkAction = linkAction;
    }
    
    /// <summary>
    /// Identifies if the event add- or removes a link between a <see cref="Contract"/>
    /// and <see cref="ModuleReferenceEvent"/>.
    /// </summary>
    public enum ModuleReferenceContractLinkAction
    {
        Added,
        Removed
    }
}
