namespace Application.Api.GraphQL.Bakers;

public class CommissionRates
{
    public decimal TransactionCommission { get; set; }
    public decimal FinalizationCommission { get; set; }
    public decimal BakingCommission { get; set; }
}