using Application.Api.GraphQL;

namespace Tests.TestUtilities.Builders.GraphQL;

public class RewardParametersV1Builder
{
    private MintDistributionV1 _mintDistribution = new() { BakingReward = 0.3m, FinalizationReward = 0.4m };
    private TransactionFeeDistribution _transactionFeeDistribution = new() {Baker = 0.5m, GasAccount = 0.6m};
    private GasRewards _gasRewards = new() { Baker = 0.21m, FinalizationProof = 0.22m, AccountCreation = 0.23m, ChainUpdate = 0.24m };

    public RewardParametersV1 Build()
    {
        return new RewardParametersV1
        {
            MintDistribution = _mintDistribution,
            TransactionFeeDistribution = _transactionFeeDistribution,
            GasRewards = _gasRewards
        };
    }

    public RewardParametersV1Builder WithMintDistribution(decimal bakingReward, decimal finalizationReward)
    {
        _mintDistribution = new()
        {
            BakingReward = bakingReward, 
            FinalizationReward = finalizationReward
        };
        return this;
    }
    
    public RewardParametersV1Builder WithTransactionFeeDistribution(decimal baker, decimal gasAccount)
    {
        _transactionFeeDistribution = new() { Baker = baker, GasAccount = gasAccount };
        return this;
    }
    
    public RewardParametersV1Builder WithGasRewards(decimal baker, decimal finalizationProof, decimal accountCreation, decimal chainUpdate)
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