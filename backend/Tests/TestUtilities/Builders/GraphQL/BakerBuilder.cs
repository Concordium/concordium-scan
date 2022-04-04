using Application.Api.GraphQL.Bakers;

namespace Tests.TestUtilities.Builders.GraphQL;

public class BakerBuilder
{
    private long _id = 10;
    private BakerState _bakerState = new ActiveBakerStateBuilder().Build();

    public Baker Build()
    {
        return new Baker
        {
            Id = _id,
            State = _bakerState
        };
    }

    public BakerBuilder WithId(long value)
    {
        _id = value;
        return this;
    }

    public BakerBuilder WithState(BakerState value)
    {
        _bakerState = value;
        return this;
    }
}