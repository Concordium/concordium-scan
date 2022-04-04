using Application.Api.GraphQL.Bakers;

namespace Tests.TestUtilities.Builders.GraphQL;

public class RemovedBakerStateBuilder
{
    public RemovedBakerState Build()
    {
        return new RemovedBakerState(new DateTimeOffset(2022, 04, 01, 12, 55, 11, 313, TimeSpan.Zero));
    }
}