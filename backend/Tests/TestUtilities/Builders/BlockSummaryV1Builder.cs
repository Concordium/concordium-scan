using Concordium.Sdk.Types.New;

namespace Tests.TestUtilities.Builders;

public class BlockSummaryV1Builder
{
    private TransactionSummary[] _transactionSummaries;
    private SpecialEvent[] _specialEvents;
    private FinalizationData? _finalizationData;
    private ChainParametersV1 _chainParameters;
    private PendingUpdatesV1 _pendingUpdates;
    private UpdateKeysCollectionV1 _updateKeysCollection;

    public BlockSummaryV1Builder()
    {
        _transactionSummaries = Array.Empty<TransactionSummary>();
        _specialEvents = Array.Empty<SpecialEvent>();
        _finalizationData = null;
        _chainParameters = new ChainParametersV1Builder().Build();
        _pendingUpdates = new PendingUpdatesV1Builder().Build();
        _updateKeysCollection = new UpdateKeysCollectionV1Builder().Build();
    }

    public BlockSummaryV1 Build()
    {
        return new BlockSummaryV1
        {
            SpecialEvents = _specialEvents,
            TransactionSummaries = _transactionSummaries,
            FinalizationData = _finalizationData,
            Updates = new UpdatesV1(_updateKeysCollection, null, _chainParameters, _pendingUpdates)
        };
    }

    public BlockSummaryV1Builder WithTransactionSummaries(params TransactionSummary[] value)
    {
        _transactionSummaries = value;
        return this;
    }

    public BlockSummaryV1Builder WithSpecialEvents(params SpecialEvent[] value)
    {
        _specialEvents = value;
        return this;
    }

    public BlockSummaryV1Builder WithFinalizationData(FinalizationData? value)
    {
        _finalizationData = value;
        return this;
    }

    public BlockSummaryV1Builder WithChainParameters(ChainParametersV1 value)
    {
        _chainParameters = value;
        return this;
    }
}