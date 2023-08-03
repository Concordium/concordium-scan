using Concordium.Sdk.Types;

namespace Tests.TestUtilities.Builders;

public class FinalizationSummaryPartyBuilder
{
    private BakerId _bakerId = new(new AccountIndex(9));
    private ulong _weight = 57366388920UL;
    private bool _signed = true;
    
    public FinalizationSummaryParty Build() => new(_bakerId, _weight, _signed);

    public FinalizationSummaryPartyBuilder WithBakerId(BakerId value)
    {
        _bakerId = value;
        return this;
    }

    public FinalizationSummaryPartyBuilder WithWeight(ulong value)
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