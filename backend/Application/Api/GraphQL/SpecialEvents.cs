using HotChocolate;

namespace Application.Api.GraphQL;

public class SpecialEvents
{
    /// <summary>
    /// This property is intentionally not part of the GraphQL schema.
    /// Only here as a back reference to the owning block so that child data can be loaded.
    /// </summary>
    [GraphQLIgnore]
    public Block Owner { get; set; }
    public Mint? Mint { get; init; }
    public FinalizationRewards? FinalizationRewards { get; init; }
    public BlockRewards? BlockRewards { get; init; }
    public BakingRewards? BakingRewards { get; init; }
}