namespace Application.Api.GraphQL.Bakers;

public class BakerPool
{
    public BakerPoolOpenStatus OpenStatus { get; init; }
    public BakerPoolCommissionRates CommissionRates { get; init; }
    public string MetadataUrl { get; init; }
}