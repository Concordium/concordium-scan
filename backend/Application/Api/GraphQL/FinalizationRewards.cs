using HotChocolate.Types;

namespace Application.Api.GraphQL;

public class FinalizationRewards
{
    public long Remainder { get; init; }
    [UsePaging]
    public FinalizationReward[] Rewards { get; init; }
}