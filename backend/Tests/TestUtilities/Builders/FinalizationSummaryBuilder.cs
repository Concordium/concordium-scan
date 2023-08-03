using System.Collections.Generic;
using Concordium.Sdk.Types;

namespace Tests.TestUtilities.Builders;

public class FinalizationSummaryBuilder
{
    private BlockHash _blockPointer = BlockHash.From("3ecb4365b81501ea41d9a07229629e6214d5043c4e5fe870f75c6808a7c2d13d");
    private ulong _index = 22UL;
    private ulong _delay = 11UL;
    private IList<FinalizationSummaryParty> _finalizers = new List<FinalizationSummaryParty>();
    
    public FinalizationSummary Build()
    {
        return new FinalizationSummary(
            _blockPointer,
            _index,
            _delay,
            _finalizers.ToArray()
        );
    }
    
    public FinalizationSummaryBuilder WithBlockPointer(BlockHash value)
    {
        _blockPointer = value;
        return this;
    }

    public FinalizationSummaryBuilder WithIndex(ulong value)
    {
        _index = value;
        return this;
    }

    public FinalizationSummaryBuilder WithDelay(ulong value)
    {
        _delay = value;
        return this;
    }
    
    public FinalizationSummaryBuilder AddSummaryParty(params FinalizationSummaryParty[] value)
    {
        foreach (var finalizationSummaryParty in value)
        {
            _finalizers.Add(finalizationSummaryParty);
        }
        return this;
    }
}
