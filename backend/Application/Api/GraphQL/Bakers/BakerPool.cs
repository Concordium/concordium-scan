namespace Application.Api.GraphQL.Bakers;

public class BakerPool
{
    public BakerPoolOpenStatus OpenStatus { get; set; }
    public BakerPoolCommissionRates CommissionRates { get; init; }
    public string MetadataUrl { get; set; }
}