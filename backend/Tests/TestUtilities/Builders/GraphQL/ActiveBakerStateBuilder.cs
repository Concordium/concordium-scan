using Application.Api.GraphQL.Bakers;

namespace Tests.TestUtilities.Builders.GraphQL;

public class ActiveBakerStateBuilder
{
    private PendingBakerChange? _pendingChange = null;
    private bool _restakeRewards = false;
    private ulong _stakedAmount = 0;

    public ActiveBakerStateBuilder WithStakedAmount(ulong value)
    {
        _stakedAmount = value;
        return this;
    }
    
    public ActiveBakerStateBuilder WithPendingChange(PendingBakerChange? value)
    {
        _pendingChange = value;
        return this;
    }

    public ActiveBakerStateBuilder WithRestakeRewards(bool value)
    {
        _restakeRewards = value;
        return this;
    }
    
    public ActiveBakerState Build()
    {
        return new ActiveBakerState(_stakedAmount, _restakeRewards, _pendingChange);
    }
}