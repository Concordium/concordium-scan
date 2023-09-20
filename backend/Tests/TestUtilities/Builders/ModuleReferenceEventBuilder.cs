using System.Collections.Generic;
using Application.Aggregates.Contract.Entities;
using Application.Aggregates.Contract.Types;
using Application.Api.GraphQL;
using Application.Api.GraphQL.Accounts;

namespace Tests.TestUtilities.Builders;

internal sealed class ModuleReferenceEventBuilder
{
    private const ulong BlockHeight = 1;
    private const string TransactionHash = "";
    private const ulong TransactionIndex = 1;
    private const uint EventIndex = 1;
    private string _moduleReference = "";
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
            BlockHeight,
            TransactionHash,
            TransactionIndex,
            EventIndex,
            _moduleReference,
            _accountAddress,
            Source,
            _dateTimeOffset
        )
        {
            ModuleReferenceContractLinkEvents = _moduleReferenceContractLinkEvents
        };
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
