using System.Threading.Tasks;
using Application.Api.GraphQL.Accounts;
using Application.Api.GraphQL.EfCore;
using Application.Common.Diagnostics;
using ConcordiumSdk.NodeApi.Types;
using Microsoft.EntityFrameworkCore;

namespace Application.Api.GraphQL.Import;

public class ChainParametersWriter
{
    private readonly IDbContextFactory<GraphQlDbContext> _dbContextFactory;
    private readonly IMetrics _metrics;
    private Account? _lastReadFoundationAccount;

    public ChainParametersWriter(IDbContextFactory<GraphQlDbContext> dbContextFactory, IMetrics metrics)
    {
        _dbContextFactory = dbContextFactory;
        _metrics = metrics;
    }

    public async Task<ChainParameters> GetOrCreateChainParameters(BlockSummaryBase blockSummary, ImportState importState)
    {
        using var counter = _metrics.MeasureDuration(nameof(ChainParametersWriter), nameof(GetOrCreateChainParameters));

        await using var context = await _dbContextFactory.CreateDbContextAsync();

        if (importState.LatestWrittenChainParameters == null)
            importState.LatestWrittenChainParameters = await context.ChainParameters
                .AsNoTracking()
                .OrderByDescending(x => x.Id)
                .FirstOrDefaultAsync();

        var lastWritten = importState.LatestWrittenChainParameters;
        var foundationAccountAddress = await GetFoundationAccountAddress(blockSummary, context);
        if (lastWritten != null)
        {
            var mappedWithLatestId = MapChainParameters(blockSummary, foundationAccountAddress, lastWritten.Id);
            if (lastWritten.Equals(mappedWithLatestId))
                return lastWritten;
        }

        var mapped = MapChainParameters(blockSummary, foundationAccountAddress);
        context.ChainParameters.Add(mapped);
        await context.SaveChangesAsync();

        importState.LatestWrittenChainParameters = mapped;
        return mapped;
    }

    private async Task<AccountAddress> GetFoundationAccountAddress(BlockSummaryBase blockSummary, GraphQlDbContext dbContext)
    {
        var currentFoundationAccountId = blockSummary switch
        {
            BlockSummaryV0 v0 => (long)v0.Updates.ChainParameters.FoundationAccountIndex,
            BlockSummaryV1 v1 => (long)v1.Updates.ChainParameters.FoundationAccountIndex,
            _ => throw new NotImplementedException()
        };

        if (_lastReadFoundationAccount != null && currentFoundationAccountId == _lastReadFoundationAccount.Id)
            return _lastReadFoundationAccount.CanonicalAddress;

        _lastReadFoundationAccount = await dbContext.Accounts
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == currentFoundationAccountId);

        if (_lastReadFoundationAccount == null)
            throw new InvalidOperationException(
                "Could not find the account in the database which was identified as foundation account in chain parameters.");

        return _lastReadFoundationAccount.CanonicalAddress;
    }

    private ChainParameters MapChainParameters(BlockSummaryBase blockSummary, AccountAddress foundationAccountAddress, int id = default)
    {
        return blockSummary switch
        {
            BlockSummaryV0 x => MapChainParameters(x.Updates.ChainParameters, foundationAccountAddress, id),
            BlockSummaryV1 x => MapChainParameters(x.Updates.ChainParameters, foundationAccountAddress, id),
            _ => throw new NotImplementedException()
        };
    }

    private ChainParameters MapChainParameters(ConcordiumSdk.NodeApi.Types.ChainParametersV0 input, AccountAddress foundationAccountAddress, int id = default)
    {
        var mintDistribution = input.RewardParameters.MintDistribution;
        var transactionFeeDistribution = input.RewardParameters.TransactionFeeDistribution;
        var gasRewards = input.RewardParameters.GASRewards;

        var rewardParameters = new RewardParametersV0
        {
            MintDistribution = new MintDistributionV0
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

        return new ChainParametersV0
        {
            Id = id,
            ElectionDifficulty = input.ElectionDifficulty,
            EuroPerEnergy = MapExchangeRate(input.EuroPerEnergy),
            MicroCcdPerEuro = MapExchangeRate(input.MicroGTUPerEuro),
            BakerCooldownEpochs = input.BakerCooldownEpochs,
            AccountCreationLimit = input.AccountCreationLimit,
            RewardParameters = rewardParameters,
            FoundationAccountId = (long)input.FoundationAccountIndex,
            FoundationAccountAddress = foundationAccountAddress,
            MinimumThresholdForBaking = input.MinimumThresholdForBaking.MicroCcdValue
        };
    }
    
    private ChainParameters MapChainParameters(ConcordiumSdk.NodeApi.Types.ChainParametersV1 input, AccountAddress foundationAccountAddress, int id = default)
    {
        var mintDistribution = input.RewardParameters.MintDistribution;
        var transactionFeeDistribution = input.RewardParameters.TransactionFeeDistribution;
        var gasRewards = input.RewardParameters.GASRewards;

        var rewardParameters = new RewardParametersV1
        {
            MintDistribution = new MintDistributionV1
            {
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

        return new ChainParametersV1
        {
            Id = id,
            ElectionDifficulty = input.ElectionDifficulty,
            EuroPerEnergy = MapExchangeRate(input.EuroPerEnergy),
            MicroCcdPerEuro = MapExchangeRate(input.MicroGTUPerEuro),
            PoolOwnerCooldown = input.PoolOwnerCooldown,
            DelegatorCooldown = input.DelegatorCooldown,
            RewardPeriodLength = input.RewardPeriodLength,
            MintPerPayday = input.MintPerPayday,
            AccountCreationLimit = input.AccountCreationLimit,
            RewardParameters = rewardParameters,
            FoundationAccountId = (long)input.FoundationAccountIndex,
            FoundationAccountAddress = foundationAccountAddress,
            FinalizationCommissionLPool = input.FinalizationCommissionLPool,
            BakingCommissionLPool = input.BakingCommissionLPool,
            TransactionCommissionLPool = input.TransactionCommissionLPool,
            FinalizationCommissionRange = MapCommissionRange(input.FinalizationCommissionRange),
            BakingCommissionRange = MapCommissionRange(input.BakingCommissionRange),
            TransactionCommissionRange = MapCommissionRange(input.TransactionCommissionRange),
            MinimumEquityCapital = input.MinimumEquityCapital.MicroCcdValue,
            CapitalBound = input.CapitalBound,
            LeverageBound = new()
            {
                Numerator = input.LeverageBound.Numerator,
                Denominator = input.LeverageBound.Denominator
            }
        };
    }

    private static CommissionRange MapCommissionRange(InclusiveRange<decimal> src)
    {
        return new () { Min = src.Min, Max = src.Max};
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