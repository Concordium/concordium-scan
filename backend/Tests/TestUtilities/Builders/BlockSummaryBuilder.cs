using ConcordiumSdk.NodeApi.Types;

namespace Tests.TestUtilities.Builders;

public class BlockSummaryBuilder
{
    private TransactionSummary[] _transactionSummaries;
    private SpecialEvent[] _specialEvents;
    private FinalizationData? _finalizationData;
    private ChainParameters _chainParameters;
    private PendingUpdates _pendingUpdates;
    private UpdateKeysCollection _updateKeysCollection;

    public BlockSummaryBuilder()
    {
        _transactionSummaries = Array.Empty<TransactionSummary>();
        _specialEvents = Array.Empty<SpecialEvent>();
        _finalizationData = null;
        _chainParameters = new ChainParametersBuilder().Build();
        _pendingUpdates = new PendingUpdatesBuilder().Build();
        _updateKeysCollection = new UpdateKeysCollectionBuilder().Build();
    }

    public BlockSummary Build()
    {
        return new BlockSummary
        {
            SpecialEvents = _specialEvents,
            TransactionSummaries = _transactionSummaries,
            FinalizationData = _finalizationData,
            Updates = new Updates(_updateKeysCollection, null, _chainParameters, _pendingUpdates)
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

    public BlockSummaryBuilder WithChainParameters(ChainParameters value)
    {
        _chainParameters = value;
        return this;
    }
}