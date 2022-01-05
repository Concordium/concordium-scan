using ConcordiumSdk.NodeApi.Types;

namespace Tests.TestUtilities.Builders;

public class FinalizationSummaryPartyBuilder
{
    private int _bakerId = 9;
    private long _weight = 57366388920;
    private bool _signed = true;

    public FinalizationSummaryParty Build()
    {
        return new FinalizationSummaryParty
        {
            BakerId = _bakerId,
            Weight = _weight,
            Signed = _signed
        };
    }

    public FinalizationSummaryPartyBuilder WithBakerId(int value)
    {
        _bakerId = value;
        return this;
    }

    public FinalizationSummaryPartyBuilder WithWeight(long value)
    {
        _weight = value;
        return this;
    }

    public FinalizationSummaryPartyBuilder WithSigned(bool value)
    {
        _signed = value;
        return this;
    }
}