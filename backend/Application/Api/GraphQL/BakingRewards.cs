using HotChocolate.Types;

namespace Application.Api.GraphQL;

public class BakingRewards
{
    public long Remainder { get; init; }
    [UsePaging(InferConnectionNameFromField = false)]
    public IEnumerable<BakingReward> Rewards { get; init; }
}