using HotChocolate;

namespace Application.Api.GraphQL.Bakers;

public class BakerPool
{
    public BakerPoolOpenStatus OpenStatus { get; set; }
    public BakerPoolCommissionRates CommissionRates { get; init; }
    public string MetadataUrl { get; set; }
    
    [GraphQLDescription("The total amount staked in this baker pool. Includes both baker stake and delegated stake.")]
    public ulong TotalStake { get; init; }
    
    [GraphQLDescription("The total amount staked by delegation to this baker pool.")]
    public ulong DelegatedStake { get; init; }
}