using Application.Api.GraphQL.Bakers;

namespace Tests.TestUtilities.Builders.GraphQL;

public class ActiveBakerStateBuilder
{
    private PendingBakerChange? _pendingChange = null;
    private bool _restakeRewards = false;

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
        return new ActiveBakerState(_restakeRewards, _pendingChange);
    }
}