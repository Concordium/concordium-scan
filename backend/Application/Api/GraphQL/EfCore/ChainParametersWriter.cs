using System.Threading.Tasks;
using ConcordiumSdk.NodeApi.Types;
using Microsoft.EntityFrameworkCore;

namespace Application.Api.GraphQL.EfCore;

public class ChainParametersWriter
{
    private readonly IDbContextFactory<GraphQlDbContext> _dbContextFactory;

    public ChainParametersWriter(IDbContextFactory<GraphQlDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }
    
    public async Task<ChainParameters> GetOrCreateChainParameters(BlockSummary blockSummary)
    {
        var mapped = MapChainParameters(blockSummary.Updates.ChainParameters);
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        context.ChainParameters.Add(mapped);
        await context.SaveChangesAsync();
        return mapped;
    }

    private ChainParameters MapChainParameters(ConcordiumSdk.NodeApi.Types.ChainParameters input)
    {
        var mintDistribution = input.RewardParameters.MintDistribution;
        var transactionFeeDistribution = input.RewardParameters.TransactionFeeDistribution;
        var gasRewards = input.RewardParameters.GASRewards;

        var rewardParameters = new RewardParameters
        {
            MintDistribution = new MintDistribution
            {
                MintPerSlot = mintDistribution.MintPerSlot, 
                BakingReward = mintDistribution.BakingReward,
                FinalizationReward = mintDistribution.FinalizationReward
            },
            TransactionFeeDistribution = new TransactionFeeDistribution
            {
                Baker = transactionFeeDistribution.Baker,
                GasAccount = transactionFeeDistribution.GasAccount
            },
            GasRewards = new GasRewards
            {
                Baker = gasRewards.Baker,
                FinalizationProof = gasRewards.FinalizationProof,
                AccountCreation = gasRewards.AccountCreation,
                ChainUpdate = gasRewards.ChainUpdate
            }
        };
        
        return new ChainParameters
        {
            ElectionDifficulty = input.ElectionDifficulty, 
            EuroPerEnergy = MapExchangeRate(input.EuroPerEnergy),
            MicroCcdPerEuro = MapExchangeRate(input.MicroGTUPerEuro), 
            BakerCooldownEpochs = input.BakerCooldownEpochs, 
            CredentialsPerBlockLimit = input.CredentialsPerBlockLimit, 
            RewardParameters = rewardParameters, 
            FoundationAccountId = (long)input.FoundationAccountIndex, 
            MinimumThresholdForBaking = input.MinimumThresholdForBaking.MicroCcdValue        
        };
    }

    private static ExchangeRate MapExchangeRate(ConcordiumSdk.NodeApi.Types.ExchangeRate input)
    {
        return new ExchangeRate
        {
            Numerator = input.Numerator,
            Denominator = input.Denominator
        };
    }
}