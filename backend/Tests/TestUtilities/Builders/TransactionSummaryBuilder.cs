using System.Text.Json;
using ConcordiumSdk.NodeApi.Types;
using ConcordiumSdk.Types;

namespace Tests.TestUtilities.Builders;

public class TransactionSummaryBuilder
{
    private TransactionType _type = TransactionType.Get(AccountTransactionType.SimpleTransfer);
    private int _index = 0;
    private TransactionResult _result = new TransactionSuccessResult() { Events = JsonDocument.Parse("[]").RootElement };
    private AccountAddress? _sender = new("31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd");
    private TransactionHash _transactionHash = new("42B83D2BE10B86BD6DF5C102C4451439422471BC4443984912A832052FF7485B");
    private CcdAmount _cost = CcdAmount.FromCcd(10);
    private int _energyCost = 100;

    public TransactionSummary Build()
    {
        return new TransactionSummary(
            _sender,
            _transactionHash,
            _cost,
            _energyCost,
            _type,
            _result,
            _index
        );
    }

    public TransactionSummaryBuilder WithType(TransactionType value)
    {
        _type = value;
        return this;
    }

    public TransactionSummaryBuilder WithIndex(int value)
    {
        _index = value;
        return this;
    }

    public TransactionSummaryBuilder WithResult(TransactionResult value)
    {
        _result = value;
        return this;
    }

    public TransactionSummaryBuilder WithSender(AccountAddress? sender)
    {
        _sender = sender;
        return this;
    }

    public TransactionSummaryBuilder WithTransactionHash(TransactionHash value)
    {
        _transactionHash = value;
        return this;
    }

    public TransactionSummaryBuilder WithCost(CcdAmount value)
    {
        _cost = value;
        return this;
    }

    public TransactionSummaryBuilder WithEnergyCost(int value)
    {
        _energyCost = value;
        return this;
    }
}