namespace Application.Api.GraphQL;

public class SpecialEvents
{
    public Mint? Mint { get; init; }
    public FinalizationRewards? FinalizationRewards { get; init; }
    public BlockRewards? BlockRewards { get; init; }
    public BakingRewards? BakingRewards { get; init; }
}