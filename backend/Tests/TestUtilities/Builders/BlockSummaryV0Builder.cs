namespace Tests.TestUtilities.Builders;

public class BlockSummaryV0Builder
{
    private TransactionSummary[] _transactionSummaries;
    private SpecialEvent[] _specialEvents;
    private FinalizationData? _finalizationData;
    private ChainParametersV0 _chainParameters;
    private PendingUpdatesV0 _pendingUpdates;
    private UpdateKeysCollectionV0 _updateKeysCollection;

    public BlockSummaryV0Builder()
    {
        _transactionSummaries = Array.Empty<TransactionSummary>();
        _specialEvents = Array.Empty<SpecialEvent>();
        _finalizationData = null;
        _chainParameters = new ChainParametersV0Builder().Build();
        _pendingUpdates = new PendingUpdatesV0Builder().Build();
        _updateKeysCollection = new UpdateKeysCollectionV0Builder().Build();
    }

    public BlockSummaryV0 Build()
    {
        return new BlockSummaryV0
        {
            SpecialEvents = _specialEvents,
            TransactionSummaries = _transactionSummaries,
            FinalizationData = _finalizationData,
            Updates = new UpdatesV0(_updateKeysCollection, null, _chainParameters, _pendingUpdates)
        };
    }

    public BlockSummaryV0Builder WithTransactionSummaries(params TransactionSummary[] value)
    {
        _transactionSummaries = value;
        return this;
    }

    public BlockSummaryV0Builder WithSpecialEvents(params SpecialEvent[] value)
    {
        _specialEvents = value;
        return this;
    }

    public BlockSummaryV0Builder WithFinalizationData(FinalizationData? value)
    {
        _finalizationData = value;
        return this;
    }

    public BlockSummaryV0Builder WithChainParameters(ChainParametersV0 value)
    {
        _chainParameters = value;
        return this;
    }
}