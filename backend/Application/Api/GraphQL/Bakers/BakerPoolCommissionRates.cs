namespace Application.Api.GraphQL.Bakers;

public class BakerPoolCommissionRates
{
    public decimal TransactionCommission { get; init; }
    public decimal FinalizationCommission { get; init; }
    public decimal BakingCommission { get; init; }
}