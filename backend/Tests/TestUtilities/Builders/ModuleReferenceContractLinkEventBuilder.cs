using Application.Aggregates.Contract.Entities;
using Application.Aggregates.Contract.Types;
using Application.Api.GraphQL;
using Application.Api.GraphQL.Accounts;

namespace Tests.TestUtilities.Builders;

internal class ModuleReferenceContractLinkEventBuilder
{
    private ulong _blockHeight = 1;
    private readonly string _transactionHash = "";
    private ulong _transactionIndex = 1;
    private uint _eventIndex = 1;
    private string _moduleReference = "";
    private ContractAddress _contractAddress = new(1, 0);
    private readonly AccountAddress _accountAddress = new("");
    private readonly ImportSource _source = ImportSource.DatabaseImport;
    private ModuleReferenceContractLinkEvent.ModuleReferenceContractLinkAction _action =
        ModuleReferenceContractLinkEvent.ModuleReferenceContractLinkAction.Added;
    private readonly DateTimeOffset _dateTimeOffset = DateTimeOffset.UtcNow;
    
    private ModuleReferenceContractLinkEventBuilder() {}

    internal static ModuleReferenceContractLinkEventBuilder Create()
    {
        return new ModuleReferenceContractLinkEventBuilder();
    }

    internal ModuleReferenceContractLinkEvent Build()
    {
        return new ModuleReferenceContractLinkEvent(
            _blockHeight,
            _transactionHash,
            _transactionIndex,
            _eventIndex,
            _moduleReference,
            _contractAddress,
            _accountAddress,
            _source,
            _action,
            _dateTimeOffset
        );
    }
    
    internal ModuleReferenceContractLinkEventBuilder WithModuleReference(
        string moduleReference)
    {
        _moduleReference = moduleReference;
        return this;
    }
    
    internal ModuleReferenceContractLinkEventBuilder WithBlockHeight(
        ulong blockHeight)
    {
        _blockHeight = blockHeight;
        return this;
    }
    
    internal ModuleReferenceContractLinkEventBuilder WithTransactionIndex(
        ulong transactionIndex)
    {
        _transactionIndex = transactionIndex;
        return this;
    }
    
    internal ModuleReferenceContractLinkEventBuilder WithEventIndex(
        uint eventIndex)
    {
        _eventIndex = eventIndex;
        return this;
    }

    internal ModuleReferenceContractLinkEventBuilder WithLinkAction(
        ModuleReferenceContractLinkEvent.ModuleReferenceContractLinkAction action)
    {
        _action = action;
        return this;
    }
}