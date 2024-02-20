using System.Collections.Generic;
using Application.Aggregates.Contract.Entities;
using Application.Aggregates.Contract.Types;
using Application.Api.GraphQL;
using Application.Api.GraphQL.Accounts;

namespace Tests.TestUtilities.Builders;

internal sealed class ContractBuilder
{
    private readonly ulong _blockHeight = 1;
    private readonly string _transactionHash = "";
    private readonly ulong _transactionIndex = 1;
    private readonly uint _eventIndex = 1;
    private ContractAddress _contractAddress = new(1, 0);
    private readonly AccountAddress _accountAddress = new("");
    private readonly ImportSource _source = ImportSource.DatabaseImport;
    private readonly DateTimeOffset _dateTimeOffset = DateTimeOffset.UtcNow;
    private IList<ModuleReferenceContractLinkEvent> _moduleReferenceContractLinkEvents = new List<ModuleReferenceContractLinkEvent>();
    private IList<ContractEvent> _contractEvents = new List<ContractEvent>();

    private ContractBuilder() {}

    internal static ContractBuilder Create()
    {
        return new ContractBuilder();
    }

    internal Contract Build()
    {
        return new Contract(
            _blockHeight,
            _transactionHash,
            _transactionIndex,
            _eventIndex,
            _contractAddress,
            _accountAddress,
            _source,
            _dateTimeOffset
        )
        {
            ModuleReferenceContractLinkEvents = _moduleReferenceContractLinkEvents,
            ContractEvents = _contractEvents
        };
    }

    internal ContractBuilder WithContractEvents(IList<ContractEvent> events)
    {
        _contractEvents = events;
        return this;
    }

    internal ContractBuilder WithModuleReferenceContractLinkEvents(IList<ModuleReferenceContractLinkEvent> events)
    {
        _moduleReferenceContractLinkEvents = events;
        return this;
    }

    internal ContractBuilder WithContractAddress(ContractAddress contractAddress)
    {
        _contractAddress = contractAddress;
        return this;
    }

} 
