using Application.Api.GraphQL.Bakers;

namespace Tests.TestUtilities.Builders.GraphQL;

public class BakerPoolBuilder
{
    private BakerPoolOpenStatus _openStatus = BakerPoolOpenStatus.OpenForAll;
    private string _metadataUrl = "https://example.com/baker-pool-details";
    private decimal _transactionCommission = 0.1m;
    private decimal _finalizationCommission = 0.1m;
    private decimal _bakingCommission = 0.1m;
    private ulong _delegatedStake = 0UL;
    private ulong _totalStake = 0UL;
    private int _delegatorCount = 0;
    private ulong _delegatedStakeCap = 1000;

    public BakerPool Build()
    {
        return new BakerPool
        {
            OpenStatus = _openStatus,
            MetadataUrl = _metadataUrl,
            CommissionRates = new CommissionRates
            {
                TransactionCommission = _transactionCommission,
                FinalizationCommission = _finalizationCommission,
                BakingCommission = _bakingCommission
            },
            DelegatedStake = _delegatedStake,
            DelegatedStakeCap = _delegatedStakeCap,
            TotalStake = _totalStake,
            DelegatorCount = _delegatorCount
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

    public BakerPoolBuilder WithDelegatedStake(ulong value)
    {
        _delegatedStake = value;
        return this;
    }

    public BakerPoolBuilder WithTotalStake(ulong value)
    {
        _totalStake = value;
        return this;
    }
    public BakerPoolBuilder WithDelegatorCount(int value)
    {
        _delegatorCount = value;
        return this;
    }

    public BakerPoolBuilder WithDelegatedStakeCap(ulong value)
    {
        _delegatedStakeCap = value;
        return this;
    }
}