using Application.Api.GraphQL.Accounts;
using Application.Api.GraphQL.Extensions;

namespace Application.Api.GraphQL;

public class ChainParametersV1 : ChainParameters, IEquatable<ChainParametersV1>
{
    public decimal ElectionDifficulty { get; init; }
    public ulong PoolOwnerCooldown { get; init; }
    public ulong DelegatorCooldown { get; init; }
    public ulong RewardPeriodLength { get; init; }
    public decimal MintPerPayday { get; init; }
    public RewardParametersV1 RewardParameters { get; init; }
    public decimal PassiveFinalizationCommission { get; init; }
    public decimal PassiveBakingCommission { get; init; }
    public decimal PassiveTransactionCommission { get; init; }
    public CommissionRange FinalizationCommissionRange { get; init; }
    public CommissionRange BakingCommissionRange { get; init; }
    public CommissionRange TransactionCommissionRange { get; init; }
    public ulong MinimumEquityCapital { get; init; }
    public decimal CapitalBound { get; init; }
    public LeverageFactor LeverageBound { get; init; }

    public bool Equals(ChainParametersV1? other)
    {
        return other != null &&
               base.Equals(other) &&
               ElectionDifficulty == other.ElectionDifficulty &&
               PoolOwnerCooldown == other.PoolOwnerCooldown &&
               DelegatorCooldown == other.DelegatorCooldown &&
               RewardPeriodLength == other.RewardPeriodLength &&
               MintPerPayday == other.MintPerPayday &&
               RewardParameters.Equals(other.RewardParameters) &&
               PassiveFinalizationCommission == other.PassiveFinalizationCommission &&
               PassiveBakingCommission == other.PassiveBakingCommission &&
               PassiveTransactionCommission == other.PassiveTransactionCommission &&
               FinalizationCommissionRange == other.FinalizationCommissionRange &&
               BakingCommissionRange == other.BakingCommissionRange &&
               TransactionCommissionRange == other.TransactionCommissionRange &&
               MinimumEquityCapital == other.MinimumEquityCapital &&
               CapitalBound == other.CapitalBound &&
               LeverageBound == other.LeverageBound;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        return obj.GetType() == GetType() && Equals(obj as ChainParametersV1);
    }

    public override int GetHashCode()
    {
        return Id;
    }

    public static bool operator ==(ChainParametersV1? left, ChainParametersV1? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(ChainParametersV1? left, ChainParametersV1? right)
    {
        return !Equals(left, right);
    }
    
    internal static ChainParametersV1 From(Concordium.Sdk.Types.ChainParametersV1 input, int id = default)
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
            EuroPerEnergy = ExchangeRate.From(input.EuroPerEnergy),
            MicroCcdPerEuro = ExchangeRate.From(input.MicroCcdPerEuro),
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
            FinalizationCommissionRange = CommissionRange.From(input.PoolParameters.CommissionBounds.Finalization),
            BakingCommissionRange = CommissionRange.From(input.PoolParameters.CommissionBounds.Baking),
            TransactionCommissionRange = CommissionRange.From(input.PoolParameters.CommissionBounds.Transaction),
            MinimumEquityCapital = input.PoolParameters.MinimumEquityCapital.Value,
            CapitalBound = input.PoolParameters.CapitalBound.Bound.AsDecimal(),
            LeverageBound = new LeverageFactor
            {
                Numerator = input.PoolParameters.LeverageBound.Numerator,
                Denominator = input.PoolParameters.LeverageBound.Denominator
            }
        };
    }
}