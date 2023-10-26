using Application.Aggregates.Contract.Types;
using Application.Api.GraphQL;
using Application.Api.GraphQL.Accounts;
using HotChocolate;
using HotChocolate.Types;

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
    [GraphQLIgnore]
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
    
    internal const string ModuleLinkEventsSql = @"
    SELECT 
        g0.block_height as BlockHeight,
        g0.transaction_index as TransactionIndex,
        g0.event_index as EventIndex,
        g0.module_reference as ModuleReference,
        g0.contract_address_index as ContractAddressIndex,
        g0.contract_address_subindex as ContractAddressSubIndex,
        g0.link_action as LinkAction,
        g0.block_slot_time as BlockSlotTime,
        g0.created_at as CreatedAt,
        g0.sender as Sender, 
        g0.source as Source,
        g0.transaction_hash as TransactionHash
    FROM graphql_module_reference_contract_link_events AS g0
    WHERE (g0.contract_address_index = @Index) AND (g0.contract_address_subindex = @Subindex)
    ORDER BY g0.block_height DESC, g0.transaction_index DESC, g0.event_index DESC;
";    

    /// <summary>
    /// Adds additional fields to the GraphQL type <see cref="ModuleReferenceContractLinkEvent"/>.
    /// </summary>
    [ExtendObjectType(typeof(ModuleReferenceContractLinkEvent))]
    public sealed class ModuleReferenceContractLinkEventExtensions
    {
        public ContractAddress GetContractAddress([Parent] ModuleReferenceContractLinkEvent linkEvent) => 
            new(linkEvent.ContractAddressIndex, linkEvent.ContractAddressSubIndex);
    }
}
