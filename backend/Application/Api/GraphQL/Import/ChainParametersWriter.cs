using System.Threading.Tasks;
using Application.Api.GraphQL.EfCore;
using Application.Common.Diagnostics;
using ConcordiumSdk.NodeApi.Types;
using Microsoft.EntityFrameworkCore;

namespace Application.Api.GraphQL.Import;

public class ChainParametersWriter
{
    private readonly IDbContextFactory<GraphQlDbContext> _dbContextFactory;
    private readonly IMetrics _metrics;

    public ChainParametersWriter(IDbContextFactory<GraphQlDbContext> dbContextFactory, IMetrics metrics)
    {
        _dbContextFactory = dbContextFactory;
        _metrics = metrics;
    }
    
    public async Task<ChainParameters> GetOrCreateChainParameters(BlockSummary blockSummary, ImportState importState)
    {
        using var counter = _metrics.MeasureDuration(nameof(ChainParametersWriter), nameof(GetOrCreateChainParameters));

        await using var context = await _dbContextFactory.CreateDbContextAsync();
        
        if (importState.LatestWrittenChainParameters == null)
            importState.LatestWrittenChainParameters = await context.ChainParameters
                .AsNoTracking()
                .OrderByDescending(x => x.Id)
                .FirstOrDefaultAsync();

        var lastWritten = importState.LatestWrittenChainParameters;
        var foundationAccountAddress = await GetFoundationAccountAddress(blockSummary.Updates.ChainParameters, lastWritten, context);
        if (lastWritten != null)
        {
            var mappedWithLatestId = MapChainParameters(blockSummary.Updates.ChainParameters, foundationAccountAddress, lastWritten.Id);
            if (lastWritten.Equals(mappedWithLatestId))
                return lastWritten;
        }

        var mapped = MapChainParameters(blockSummary.Updates.ChainParameters, foundationAccountAddress);
        context.ChainParameters.Add(mapped);
        await context.SaveChangesAsync();
        
        importState.LatestWrittenChainParameters = mapped;
        return mapped;
    }

    private async Task<AccountAddress> GetFoundationAccountAddress(ConcordiumSdk.NodeApi.Types.ChainParameters current, ChainParameters? lastWritten, GraphQlDbContext dbContext)
    {
        var currentFoundationAccountId = (long)current.FoundationAccountIndex;
        
        if (lastWritten != null && currentFoundationAccountId == lastWritten.FoundationAccountId)
            return lastWritten.FoundationAccountAddress;
        
        var foundationAccount = await dbContext.Accounts
            .SingleOrDefaultAsync(x => x.Id == currentFoundationAccountId);

        if (foundationAccount == null)
            throw new InvalidOperationException("Could not find the account in the database which was identified as foundation account in chain parameters.");
        
        return foundationAccount.CanonicalAddress;
    }

    private ChainParameters MapChainParameters(ConcordiumSdk.NodeApi.Types.ChainParameters input, AccountAddress foundationAccountAddress, int id = default)
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
            Id = id,
            ElectionDifficulty = input.ElectionDifficulty, 
            EuroPerEnergy = MapExchangeRate(input.EuroPerEnergy),
            MicroCcdPerEuro = MapExchangeRate(input.MicroGTUPerEuro), 
            BakerCooldownEpochs = input.BakerCooldownEpochs, 
            CredentialsPerBlockLimit = input.CredentialsPerBlockLimit, 
            RewardParameters = rewardParameters, 
            FoundationAccountId = (long)input.FoundationAccountIndex, 
            FoundationAccountAddress = foundationAccountAddress,
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