using Application.Api.GraphQL.Accounts;
using Application.Api.GraphQL.Extensions;

namespace Application.Api.GraphQL;

public class ChainParametersV0 : ChainParameters, IEquatable<ChainParametersV0>
{
    public decimal ElectionDifficulty { get; init; }
    public ulong BakerCooldownEpochs { get; init; }
    public RewardParametersV0 RewardParameters { get; init; }
    public ulong MinimumThresholdForBaking { get; init; }

    public bool Equals(ChainParametersV0? other)
    {
        return other != null &&
               base.Equals(other) &&
               ElectionDifficulty == other.ElectionDifficulty &&
               BakerCooldownEpochs == other.BakerCooldownEpochs &&
               RewardParameters.Equals(other.RewardParameters) &&
               MinimumThresholdForBaking == other.MinimumThresholdForBaking;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        return obj.GetType() == GetType() && Equals(obj as ChainParametersV0);
    }

    public override int GetHashCode()
    {
        return Id;
    }

    public static bool operator ==(ChainParametersV0? left, ChainParametersV0? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(ChainParametersV0? left, ChainParametersV0? right)
    {
        return !Equals(left, right);
    }
    
    internal static ChainParametersV0 From(Concordium.Sdk.Types.ChainParametersV0 input)
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
            ElectionDifficulty = input.ElectionDifficulty.AsDecimal(),
            EuroPerEnergy = ExchangeRate.From(input.EuroPerEnergy),
            MicroCcdPerEuro = ExchangeRate.From(input.MicroCcdPerEuro),
            BakerCooldownEpochs = input.BakerCooldownEpochs.Count,
            AccountCreationLimit = (int)input.AccountCreationLimit.Limit,
            RewardParameters = rewardParameters,
            FoundationAccountAddress = AccountAddress.From(input.FoundationAccount),
            MinimumThresholdForBaking = input.MinimumThresholdForBaking.Value
        };
    }
}
