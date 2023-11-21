namespace Application.Api.GraphQL.Bakers;

public class CommissionRates
{
    public decimal TransactionCommission { get; internal set; }
    public decimal FinalizationCommission { get; internal set; }
    public decimal BakingCommission { get; internal set; }

    internal static CommissionRates From(Concordium.Sdk.Types.CommissionRates rates) =>
        new()
        {
            TransactionCommission = rates.TransactionCommission.AsDecimal(),
            BakingCommission = rates.BakingCommission.AsDecimal(),
            FinalizationCommission = rates.FinalizationCommission.AsDecimal()
        };
    
    internal void Update(Concordium.Sdk.Types.CommissionRates rates)
    {
        TransactionCommission = rates.TransactionCommission.AsDecimal();
        BakingCommission = rates.BakingCommission.AsDecimal();
        FinalizationCommission = rates.FinalizationCommission.AsDecimal();
    }
}
