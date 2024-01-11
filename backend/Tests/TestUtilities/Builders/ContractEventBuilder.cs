using Application.Aggregates.Contract.Entities;
using Application.Aggregates.Contract.Types;
using Application.Api.GraphQL;
using Application.Api.GraphQL.Accounts;
using Application.Api.GraphQL.Transactions;

namespace Tests.TestUtilities.Builders;

internal sealed class ContractEventBuilder
{
    private ulong _blockHeight = 1;
    private readonly string _transactionHash = "";
    private ulong _transactionIndex = 1;
    private uint _eventIndex = 1;
    private ContractAddress _contractAddress = new(1, 0);
    private readonly AccountAddress _accountAddress = new("");
    private TransactionResultEvent _event =
        new Transferred(1, new ContractAddress(1, 0), new ContractAddress(2, 0));

    private readonly ImportSource _source = ImportSource.DatabaseImport;
    private readonly DateTimeOffset _dateTimeOffset = DateTimeOffset.UtcNow;
    
    private ContractEventBuilder()
    {
    }

    internal static ContractEventBuilder Create()
    {
        return new ContractEventBuilder();
    }

    internal ContractEvent Build()
    {
        return new ContractEvent(
            _blockHeight,
            _transactionHash,
            _transactionIndex, 
            _eventIndex,
            _contractAddress,
            _accountAddress,
            _event,
            _source,
            _dateTimeOffset
        );
    }

    internal ContractEventBuilder WithContractAddress(ContractAddress contractAddress)
    {
        _contractAddress = contractAddress;
        return this;
    }
    
    internal ContractEventBuilder WithTransactionIndex(ulong transactionIndex)
    {
        _transactionIndex = transactionIndex;
        return this;
    }
    
    internal ContractEventBuilder WithEventIndex(uint eventIndex)
    {
        _eventIndex = eventIndex;
        return this;
    }
    
    internal ContractEventBuilder WithBlockHeight(ulong blockHeight)
    {
        _blockHeight = blockHeight;
        return this;
    }

    internal ContractEventBuilder WithEvent(TransactionResultEvent @event)
    {
        _event = @event;
        return this;
    }

}
