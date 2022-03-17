using ConcordiumSdk.NodeApi.Types;

namespace Tests.TestUtilities.Builders;

public class RewardParametersBuilder
{
    private MintDistribution _mintDistribution = new(0.2m, 0.3m, 0.4m);
    private TransactionFeeDistribution _transactionFeeDistribution = new(0.5m, 0.6m);
    private GasRewards _gasRewards = new(0.21m, 0.22m, 0.23m, 0.24m);

    public RewardParameters Build()
    {
        return new RewardParameters(_mintDistribution, _transactionFeeDistribution, _gasRewards);
    }

    public RewardParametersBuilder WithMintDistribution(decimal mintPerSlot, decimal bakingReward, decimal finalizationReward)
    {
        _mintDistribution = new MintDistribution(mintPerSlot, bakingReward, finalizationReward);
        return this;
    }
    public RewardParametersBuilder WithTransactionFeeDistribution(decimal baker, decimal gasAccount)
    {
        _transactionFeeDistribution = new TransactionFeeDistribution(baker, gasAccount);
        return this;
    }
    public RewardParametersBuilder WithGasRewards(decimal baker, decimal finalizationProof, decimal accountCreation, decimal chainUpdate)
    {
        _gasRewards = new GasRewards(baker, finalizationProof, accountCreation, chainUpdate);
        return this;
    }
}