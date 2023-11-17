namespace Application.Api.GraphQL.Bakers;

public class CommissionRates
{
    public decimal TransactionCommission { get; set; }
    public decimal FinalizationCommission { get; set; }
    public decimal BakingCommission { get; set; }

    internal static CommissionRates From(Concordium.Sdk.Types.CommissionRates rates) =>
        new()
        {
            TransactionCommission = rates.TransactionCommission.AsDecimal(),
            BakingCommission = rates.BakingCommission.AsDecimal(),
            FinalizationCommission = rates.FinalizationCommission.AsDecimal()
        };
}
