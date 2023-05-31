using Concordium.Sdk.Types;
using Concordium.Sdk.Types.New;

namespace Tests.TestUtilities.Builders;

public class FinalizationDataBuilder
{
    private BlockHash _finalizationBlockPointer = BlockHash.From("3ecb4365b81501ea41d9a07229629e6214d5043c4e5fe870f75c6808a7c2d13d");
    private long _finalizationDelay = 22;
    private long _finalizationIndex = 11;
    private FinalizationSummaryParty[] _finalizers = {
        new FinalizationSummaryPartyBuilder().WithBakerId(1).WithWeight(130).WithSigned(true).Build(),
        new FinalizationSummaryPartyBuilder().WithBakerId(2).WithWeight(220).WithSigned(false).Build()
    };

    public FinalizationData Build()
    {
        return new FinalizationData
        {
            FinalizationBlockPointer = _finalizationBlockPointer,
            FinalizationDelay = _finalizationDelay,
            FinalizationIndex = _finalizationIndex,
            Finalizers = _finalizers
        };
    }

    public FinalizationDataBuilder WithFinalizationBlockPointer(BlockHash value)
    {
        _finalizationBlockPointer = value;
        return this;
    }

    public FinalizationDataBuilder WithFinalizationIndex(long value)
    {
        _finalizationIndex = value;
        return this;
    }

    public FinalizationDataBuilder WithFinalizationDelay(long value)
    {
        _finalizationDelay = value;
        return this;
    }

    public FinalizationDataBuilder WithFinalizers(params FinalizationSummaryParty[] value)
    {
        _finalizers = value;
        return this;
    }
}