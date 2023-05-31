using Concordium.Sdk.Types.New;

namespace Tests.TestUtilities.Builders;

public class RewardParametersV1Builder
{
    private MintDistributionV1 _mintDistribution = new(0.3m, 0.4m);
    private TransactionFeeDistribution _transactionFeeDistribution = new(0.5m, 0.6m);
    private GasRewards _gasRewards = new(0.21m, 0.22m, 0.23m, 0.24m);

    public RewardParametersV1 Build()
    {
        return new RewardParametersV1(_mintDistribution, _transactionFeeDistribution, _gasRewards);
    }

    public RewardParametersV1Builder WithMintDistribution(decimal bakingReward, decimal finalizationReward)
    {
        _mintDistribution = new MintDistributionV1(bakingReward, finalizationReward);
        return this;
    }
    public RewardParametersV1Builder WithTransactionFeeDistribution(decimal baker, decimal gasAccount)
    {
        _transactionFeeDistribution = new TransactionFeeDistribution(baker, gasAccount);
        return this;
    }
    public RewardParametersV1Builder WithGasRewards(decimal baker, decimal finalizationProof, decimal accountCreation, decimal chainUpdate)
    {
        _gasRewards = new GasRewards(baker, finalizationProof, accountCreation, chainUpdate);
        return this;
    }
}