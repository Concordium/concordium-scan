using System.Threading.Tasks;
using Application.Api.GraphQL.EfCore;
using Application.Common.Diagnostics;
using Concordium.Sdk.Types;
using Microsoft.EntityFrameworkCore;
using AccountAddress = Application.Api.GraphQL.Accounts.AccountAddress;

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

    public async Task<ChainParametersState> GetOrCreateChainParameters(IChainParameters chainParameters, ImportState importState)
    {
        using var counter = _metrics.MeasureDuration(nameof(ChainParametersWriter), nameof(GetOrCreateChainParameters));

        await using var context = await _dbContextFactory.CreateDbContextAsync();
        
        importState.LatestWrittenChainParameters ??= await context.ChainParameters
            .AsNoTracking()
            .OrderByDescending(x => x.Id)
            .FirstOrDefaultAsync();

        var lastWritten = importState.LatestWrittenChainParameters;
        if (lastWritten != null)
        {
            var mappedWithLatestId = MapChainParameters(chainParameters, lastWritten.Id);
            if (lastWritten.Equals(mappedWithLatestId))
                return new ChainParametersState(lastWritten);
        }

        var mapped = MapChainParameters(chainParameters);
        context.ChainParameters.Add(mapped);
        await context.SaveChangesAsync();

        importState.LatestWrittenChainParameters = mapped;
        
        return lastWritten == null
            ? new ChainParametersState(mapped)
            : new ChainParametersChangedState(mapped, lastWritten);
    }

    private static ChainParameters MapChainParameters(IChainParameters chainParameters, int id = default)
    {
        return chainParameters switch
        {
            Concordium.Sdk.Types.ChainParametersV0 chainParametersV0 => MapChainParameters(chainParametersV0, id),
            Concordium.Sdk.Types.ChainParametersV1 chainParametersV1 => MapChainParameters(chainParametersV1, id),
            _ => throw new NotImplementedException()
        };
    }

    private static ChainParameters MapChainParameters(Concordium.Sdk.Types.ChainParametersV0 input, int id = default)
    {
        var mintDistribution = input.MintDistribution;
        var transactionFeeDistribution = input.TransactionFeeDistribution;
        var gasRewards = input.GasRewards;

        var rewardParameters = new RewardParametersV0
        {
            MintDistribution = new MintDistributionV0
            {
                MintPerSlot = mintDistribution.MintPerSlot.AsDecimal(),
                BakingReward = mintDistribution.BakingReward.AsDecimal(),
                FinalizationReward = mintDistribution.FinalizationReward.AsDecimal()
            },
            TransactionFeeDistribution = new TransactionFeeDistribution
            {
                Baker = transactionFeeDistribution.Baker.AsDecimal(),
                GasAccount = transactionFeeDistribution.GasAccount.AsDecimal()
            },
            GasRewards = new GasRewards
            {
                Baker = gasRewards.Baker.AsDecimal(),
                FinalizationProof = gasRewards.FinalizationProof.AsDecimal(),
                AccountCreation = gasRewards.AccountCreation.AsDecimal(),
                ChainUpdate = gasRewards.ChainUpdate.AsDecimal()
            }
        };

        return new ChainParametersV0
        {
            Id = id,
            ElectionDifficulty = input.ElectionDifficulty.AsDecimal(),
            EuroPerEnergy = MapExchangeRate(input.EuroPerEnergy),
            MicroCcdPerEuro = MapExchangeRate(input.MicroCcdPerEuro),
            BakerCooldownEpochs = input.BakerCooldownEpochs.Count,
            AccountCreationLimit = (int)input.AccountCreationLimit.Limit,
            RewardParameters = rewardParameters,
            FoundationAccountAddress = AccountAddress.From(input.FoundationAccount),
            MinimumThresholdForBaking = input.MinimumThresholdForBaking.Value
        };
    }
    
    private static ChainParameters MapChainParameters(Concordium.Sdk.Types.ChainParametersV1 input, int id = default)
    {
        var mintDistribution = input.MintDistribution;
        var transactionFeeDistribution = input.TransactionFeeDistribution;
        var gasRewards = input.GasRewards;

        var rewardParameters = new RewardParametersV1
        {
            MintDistribution = new MintDistributionV1
            {
                BakingReward = mintDistribution.BakingReward.AsDecimal(),
                FinalizationReward = mintDistribution.FinalizationReward.AsDecimal()
            },
            TransactionFeeDistribution = new TransactionFeeDistribution
            {
                Baker = transactionFeeDistribution.Baker.AsDecimal(),
                GasAccount = transactionFeeDistribution.GasAccount.AsDecimal()
            },
            GasRewards = new GasRewards
            {
                Baker = gasRewards.Baker.AsDecimal(),
                FinalizationProof = gasRewards.FinalizationProof.AsDecimal(),
                AccountCreation = gasRewards.AccountCreation.AsDecimal(),
                ChainUpdate = gasRewards.ChainUpdate.AsDecimal()
            }
        };

        return new ChainParametersV1
        {
            Id = id,
            ElectionDifficulty = input.ElectionDifficulty.AsDecimal(),
            EuroPerEnergy = MapExchangeRate(input.EuroPerEnergy),
            MicroCcdPerEuro = MapExchangeRate(input.MicroCcdPerEuro),
            PoolOwnerCooldown = (ulong)input.CooldownParameters.PoolOwnerCooldown.TotalSeconds,
            DelegatorCooldown = (ulong)input.CooldownParameters.DelegatorCooldown.TotalSeconds,
            RewardPeriodLength = input.TimeParameters.RewardPeriodLength.RewardPeriodEpochs.Count,
            MintPerPayday = input.TimeParameters.MintPrPayDay.AsDecimal(),
            AccountCreationLimit = (int)input.AccountCreationLimit.Limit,
            RewardParameters = rewardParameters,
            FoundationAccountAddress = AccountAddress.From(input.FoundationAccount),
            PassiveFinalizationCommission = input.PoolParameters.PassiveFinalizationCommission.AsDecimal(),
            PassiveBakingCommission = input.PoolParameters.PassiveBakingCommission.AsDecimal(),
            PassiveTransactionCommission = input.PoolParameters.PassiveTransactionCommission.AsDecimal(),
            FinalizationCommissionRange = MapCommissionRange(input.PoolParameters.CommissionBounds.Finalization),
            BakingCommissionRange = MapCommissionRange(input.PoolParameters.CommissionBounds.Baking),
            TransactionCommissionRange = MapCommissionRange(input.PoolParameters.CommissionBounds.Transaction),
            MinimumEquityCapital = input.PoolParameters.MinimumEquityCapital.Value,
            CapitalBound = input.PoolParameters.CapitalBound.Bound.AsDecimal(),
            LeverageBound = new LeverageFactor
            {
                Numerator = input.PoolParameters.LeverageBound.Numerator,
                Denominator = input.PoolParameters.LeverageBound.Denominator
            }
        };
    }

    private static CommissionRange MapCommissionRange(InclusiveRange<AmountFraction> src)
    {
        return new () { Min = src.Min.AsDecimal(), Max = src.Max.AsDecimal()};
    }

    private static ExchangeRate MapExchangeRate(Concordium.Sdk.Types.ExchangeRate input)
    {
        return new ExchangeRate
        {
            Numerator = input.Numerator,
            Denominator = input.Denominator
        };
    }
}

public record ChainParametersState(ChainParameters Current);
public record ChainParametersChangedState(ChainParameters Current, ChainParameters Previous) : ChainParametersState(Current);
