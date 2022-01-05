using ConcordiumSdk.NodeApi.Types;

namespace Tests.TestUtilities.Builders;

public class BlockSummaryBuilder
{
    private TransactionSummary[] _transactionSummaries;
    private SpecialEvent[] _specialEvents;
    private FinalizationData? _finalizationData;

    public BlockSummaryBuilder()
    {
        _transactionSummaries = new TransactionSummary[0];
        _specialEvents = new SpecialEvent[0];
        _finalizationData = null;
    }

    public BlockSummary Build()
    {
        return new BlockSummary
        {
            SpecialEvents = _specialEvents,
            TransactionSummaries = _transactionSummaries,
            FinalizationData = _finalizationData
        };
    }

    public BlockSummaryBuilder WithTransactionSummaries(params TransactionSummary[] value)
    {
        _transactionSummaries = value;
        return this;
    }

    public BlockSummaryBuilder WithSpecialEvents(params SpecialEvent[] value)
    {
        _specialEvents = value;
        return this;
    }

    public BlockSummaryBuilder WithFinalizationData(FinalizationData? value)
    {
        _finalizationData = value;
        return this;
    }
}