using Application.Api.GraphQL.Accounts;
using Application.Api.GraphQL.Extensions;

namespace Application.Api.GraphQL;

public class ChainParametersV3 : ChainParameters, IEquatable<ChainParametersV3>
{
    public ulong PoolOwnerCooldown { get; init; }
    public ulong DelegatorCooldown { get; init; }
    public ulong RewardPeriodLength { get; init; }
    public decimal MintPerPayday { get; init; }
    public RewardParametersV2 RewardParameters { get; init; }
    public decimal PassiveFinalizationCommission { get; init; }
    public decimal PassiveBakingCommission { get; init; }
    public decimal PassiveTransactionCommission { get; init; }
    public CommissionRange FinalizationCommissionRange { get; init; }
    public CommissionRange BakingCommissionRange { get; init; }
    public CommissionRange TransactionCommissionRange { get; init; }
    public ulong MinimumEquityCapital { get; init; }
    public decimal CapitalBound { get; init; }
    public LeverageFactor LeverageBound { get; init; }

    internal static ChainParametersV3 From(Concordium.Sdk.Types.ChainParametersV3 input)
    {
        var rewardParameters = new RewardParametersV2
        {
            MintDistribution = new MintDistributionV1
            {
                BakingReward = input.MintDistribution.BakingReward.AsDecimal(),
                FinalizationReward = input.MintDistribution.FinalizationReward.AsDecimal()
            },
            TransactionFeeDistribution = new TransactionFeeDistribution
            {
                Baker = input.TransactionFeeDistribution.Baker.AsDecimal(),
                GasAccount = input.TransactionFeeDistribution.GasAccount.AsDecimal()
            },
            GasRewards = new GasRewardsCpv2
            {
                Baker = input.GasRewards.Baker.AsDecimal(),
                AccountCreation = input.GasRewards.AccountCreation.AsDecimal(),
                ChainUpdate = input.GasRewards.ChainUpdate.AsDecimal()
            }
        };
        
        return new ChainParametersV3
        {
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
            },
        };

    }

    public bool Equals(ChainParametersV3? other)
    {
        return other != null &&
               base.Equals(other) &&
               RewardPeriodLength == other.RewardPeriodLength;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        return obj.GetType() == GetType() && Equals(obj as ChainParametersV3);
    }

    public override int GetHashCode()
    {
        return Id;
    }

    public static bool operator ==(ChainParametersV3? left, ChainParametersV3? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(ChainParametersV3? left, ChainParametersV3? right)
    {
        return !Equals(left, right);
    }
}
