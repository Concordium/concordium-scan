using HotChocolate.Types;

namespace Application.Api.GraphQL;

public class FinalizationRewards
{
    public long Remainder { get; init; }
    [UsePaging(InferConnectionNameFromField = false)]
    public IEnumerable<FinalizationReward> Rewards { get; init; }
}