using ConcordiumSdk.NodeApi.Types;

namespace Tests.TestUtilities.Builders;

public class RewardParametersV0Builder
{
    private MintDistributionV0 _mintDistribution = new(0.2m, 0.3m, 0.4m);
    private TransactionFeeDistribution _transactionFeeDistribution = new(0.5m, 0.6m);
    private GasRewards _gasRewards = new(0.21m, 0.22m, 0.23m, 0.24m);

    public RewardParametersV0 Build()
    {
        return new RewardParametersV0(_mintDistribution, _transactionFeeDistribution, _gasRewards);
    }

    public RewardParametersV0Builder WithMintDistribution(decimal mintPerSlot, decimal bakingReward, decimal finalizationReward)
    {
        _mintDistribution = new MintDistributionV0(mintPerSlot, bakingReward, finalizationReward);
        return this;
    }
    public RewardParametersV0Builder WithTransactionFeeDistribution(decimal baker, decimal gasAccount)
    {
        _transactionFeeDistribution = new TransactionFeeDistribution(baker, gasAccount);
        return this;
    }
    public RewardParametersV0Builder WithGasRewards(decimal baker, decimal finalizationProof, decimal accountCreation, decimal chainUpdate)
    {
        _gasRewards = new GasRewards(baker, finalizationProof, accountCreation, chainUpdate);
        return this;
    }
}