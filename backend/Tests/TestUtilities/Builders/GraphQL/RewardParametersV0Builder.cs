using Application.Api.GraphQL;

namespace Tests.TestUtilities.Builders.GraphQL;

public class RewardParametersV0Builder
{
    private MintDistributionV0 _mintDistribution = new() { MintPerSlot = 0.2m, BakingReward = 0.3m, FinalizationReward = 0.4m };
    private TransactionFeeDistribution _transactionFeeDistribution = new() {Baker = 0.5m, GasAccount = 0.6m};
    private GasRewards _gasRewards = new() { Baker = 0.21m, FinalizationProof = 0.22m, AccountCreation = 0.23m, ChainUpdate = 0.24m };

    public RewardParametersV0 Build()
    {
        return new RewardParametersV0
        {
            MintDistribution = _mintDistribution,
            TransactionFeeDistribution = _transactionFeeDistribution,
            GasRewards = _gasRewards
        };
    }

    public RewardParametersV0Builder WithMintDistribution(decimal mintPerSlot, decimal bakingReward, decimal finalizationReward)
    {
        _mintDistribution = new()
        {
            MintPerSlot = mintPerSlot, 
            BakingReward = bakingReward, 
            FinalizationReward = finalizationReward
        };
        return this;
    }
    public RewardParametersV0Builder WithTransactionFeeDistribution(decimal baker, decimal gasAccount)
    {
        _transactionFeeDistribution = new() { Baker = baker, GasAccount = gasAccount };
        return this;
    }
    public RewardParametersV0Builder WithGasRewards(decimal baker, decimal finalizationProof, decimal accountCreation, decimal chainUpdate)
    {
        _gasRewards = new()
        {
            Baker = baker, 
            FinalizationProof = finalizationProof, 
            AccountCreation = accountCreation,
            ChainUpdate = chainUpdate
        };
        return this;
    }
}