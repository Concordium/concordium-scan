using Application.Api.GraphQL.Bakers;

namespace Tests.TestUtilities.Builders.GraphQL;

public class BakerPoolBuilder
{
    private BakerPoolOpenStatus _openStatus = BakerPoolOpenStatus.OpenForAll;
    private string _metadataUrl = "https://example.com/baker-pool-details";
    private decimal _transactionCommission = 0.1m;
    private decimal _finalizationCommission = 0.1m;
    private decimal _bakingCommission = 0.1m;

    public BakerPool Build()
    {
        return new BakerPool
        {
            OpenStatus = _openStatus,
            MetadataUrl = _metadataUrl,
            CommissionRates = new BakerPoolCommissionRates
            {
                TransactionCommission = _transactionCommission,
                FinalizationCommission = _finalizationCommission,
                BakingCommission = _bakingCommission
            }
        };
    }

    public BakerPoolBuilder WithOpenStatus(BakerPoolOpenStatus value)
    {
        _openStatus = value;
        return this;
    }
    
    public BakerPoolBuilder WithMetadataUrl(string value)
    {
        _metadataUrl = value;
        return this;
    }
    
    public BakerPoolBuilder WithCommissionRates(decimal transactionCommission = 0.1m, decimal finalizationCommission = 0.1m, decimal bakingCommission = 0.1m)
    {
        _transactionCommission = transactionCommission;
        _finalizationCommission = finalizationCommission;
        _bakingCommission = bakingCommission;
        return this;
    }
}