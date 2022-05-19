using HotChocolate;

namespace Application.Api.GraphQL.Bakers;

public class BakerPool
{
    public BakerPoolOpenStatus OpenStatus { get; set; }
    public BakerPoolCommissionRates CommissionRates { get; init; }
    public string MetadataUrl { get; set; }
    
    [GraphQLDescription("The total stake delegated to this baker pool.")]
    public ulong DelegatedStake { get; set; }
}