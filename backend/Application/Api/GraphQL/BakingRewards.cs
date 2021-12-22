using HotChocolate.Types;

namespace Application.Api.GraphQL;

public class BakingRewards
{
    public long Remainder { get; init; }
    [UsePaging]
    public BakingReward[] Rewards { get; init; }
}