using System.Collections.Generic;
using Application.Aggregates.Contract.Entities;
using Application.Aggregates.Contract.Types;
using Application.Api.GraphQL;
using Application.Api.GraphQL.Accounts;

namespace Tests.TestUtilities.Builders;

internal sealed class ModuleReferenceEventBuilder
{
    private ulong _blockHeight = 1;
    private const string TransactionHash = "";
    private ulong _transactionIndex = 1;
    private uint _eventIndex = 1;
    private string _moduleReference = "";
    private const string ModuleSource = "";
    private readonly AccountAddress _accountAddress = new("");
    private const ImportSource Source = ImportSource.DatabaseImport;
    private readonly DateTimeOffset _dateTimeOffset = DateTimeOffset.UtcNow;
    private IList<ModuleReferenceContractLinkEvent> _moduleReferenceContractLinkEvents =  new List<ModuleReferenceContractLinkEvent>();
    
    private ModuleReferenceEventBuilder() {}

    internal static ModuleReferenceEventBuilder Create()
    {
        return new ModuleReferenceEventBuilder();
    }

    internal ModuleReferenceEvent Build()
    {
        return new ModuleReferenceEvent(
            _blockHeight,
            TransactionHash,
            _transactionIndex,
            _eventIndex,
            _moduleReference,
            _accountAddress,
            ModuleSource,
            null,
            null,
            Source,
            _dateTimeOffset
        )
        {
            ModuleReferenceContractLinkEvents = _moduleReferenceContractLinkEvents
        };
    }
    
    internal ModuleReferenceEventBuilder WithBlockHeight(ulong blockHeight)
    {
        _blockHeight = blockHeight;
        return this;
    }
    
    internal ModuleReferenceEventBuilder WithTransactionIndex(ulong transactionIndex)
    {
        _transactionIndex = transactionIndex;
        return this;
    }
    
    internal ModuleReferenceEventBuilder WithEventIndex(uint eventIndex)
    {
        _eventIndex = eventIndex;
        return this;
    }
    
    internal ModuleReferenceEventBuilder WithModuleReference(string moduleReference)
    {
        _moduleReference = moduleReference;
        return this;
    }

    internal ModuleReferenceEventBuilder WithModuleReferenceContractLinkEvent(
        IList<ModuleReferenceContractLinkEvent> moduleReferenceContractLinkEvents)
    {
        _moduleReferenceContractLinkEvents = moduleReferenceContractLinkEvents;
        return this;
    }
}
