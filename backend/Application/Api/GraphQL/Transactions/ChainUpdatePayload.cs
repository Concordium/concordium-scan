using Application.Api.GraphQL.Extensions;
using Concordium.Sdk.Types;
using HotChocolate;
using HotChocolate.Types;
using AccountAddress = Application.Api.GraphQL.Accounts.AccountAddress;

namespace Application.Api.GraphQL.Transactions;

[UnionType]
public abstract record ChainUpdatePayload
{
    internal static ChainUpdatePayload From(IUpdatePayload payload) =>
        payload switch
        {
            AddAnonymityRevokerUpdate addAnonymityRevokerUpdate => AddAnonymityRevokerChainUpdatePayload.From(addAnonymityRevokerUpdate),
            AddIdentityProviderUpdate addIdentityProviderUpdate => AddIdentityProviderChainUpdatePayload.From(addIdentityProviderUpdate),
            BakerStakeThresholdUpdate bakerStakeThresholdUpdate => BakerStakeThresholdChainUpdatePayload.From(bakerStakeThresholdUpdate),
            CooldownParametersCpv1Update cooldownParametersCpv1Update => CooldownParametersChainUpdatePayload.From(cooldownParametersCpv1Update),
            ElectionDifficultyUpdate electionDifficultyUpdate => ElectionDifficultyChainUpdatePayload.From(electionDifficultyUpdate),
            EuroPerEnergyUpdate euroPerEnergyUpdate => EuroPerEnergyChainUpdatePayload.From(euroPerEnergyUpdate),
            FoundationAccountUpdate foundationAccountUpdate => FoundationAccountChainUpdatePayload.From(foundationAccountUpdate),
            GasRewardsUpdate gasRewardsUpdate => GasRewardsChainUpdatePayload.From(gasRewardsUpdate),
            Level1 level1 => Level1KeysChainUpdatePayload.From(level1),
            MicroCcdPerEuroUpdate microCcdPerEuroUpdate => MicroCcdPerEuroChainUpdatePayload.From(microCcdPerEuroUpdate),
            MintDistributionCpv0Update mintDistributionCpv0Update => MintDistributionChainUpdatePayload.From(mintDistributionCpv0Update),
            MintDistributionCpv1Update mintDistributionCpv1Update => MintDistributionV1ChainUpdatePayload.From(mintDistributionCpv1Update),
            PoolParametersCpv1Update poolParametersCpv1Update => PoolParametersChainUpdatePayload.From(poolParametersCpv1Update),
            ProtocolUpdate protocolUpdate => ProtocolChainUpdatePayload.From(protocolUpdate),
            RootUpdate rootUpdate => RootKeysChainUpdatePayload.From(rootUpdate),
            TimeParametersCpv1Update timeParametersCpv1Update => TimeParametersChainUpdatePayload.From(timeParametersCpv1Update),
            TransactionFeeDistributionUpdate transactionFeeDistributionUpdate => TransactionFeeDistributionChainUpdatePayload.From(transactionFeeDistributionUpdate),
            Concordium.Sdk.Types.GasRewardsCpv2Update update => GasRewardsCpv2Update.From(update),
            Concordium.Sdk.Types.BlockEnergyLimitUpdate update => BlockEnergyLimitUpdate.From(update),
            Concordium.Sdk.Types.FinalizationCommitteeParametersUpdate update => FinalizationCommitteeParametersUpdate.From(update),
            Concordium.Sdk.Types.TimeoutParametersUpdate update => TimeoutParametersUpdate.From(update),
            Concordium.Sdk.Types.MinBlockTimeUpdate update => MinBlockTimeUpdate.From(update),
            Concordium.Sdk.Types.ValidatorScoreParametersUpdate update => ValidatorScoreParametersUpdate.From(update),
            _ => throw new ArgumentOutOfRangeException(nameof(payload))
        };
}

public sealed record MinBlockTimeUpdate(ulong DurationSeconds) : ChainUpdatePayload
{
    internal static MinBlockTimeUpdate From(Concordium.Sdk.Types.MinBlockTimeUpdate update) =>
        new((ulong)update.Duration.TotalSeconds);

}

public sealed record ValidatorScoreParametersUpdate(ulong MaximumMissedRounds) : ChainUpdatePayload
{
    internal static ValidatorScoreParametersUpdate From(Concordium.Sdk.Types.ValidatorScoreParametersUpdate update) =>
        new(update.ValidatorScoreParameters.MaximumMissedRounds);

}

public sealed record TimeoutParametersUpdate(
    ulong DurationSeconds, Ratio Increase, Ratio Decrease
    ) : ChainUpdatePayload
{
    internal static TimeoutParametersUpdate From(Concordium.Sdk.Types.TimeoutParametersUpdate update) =>
        new(
            (ulong)update.TimeoutParameters.Duration.TotalSeconds,
            Ratio.From(update.TimeoutParameters.Increase),
            Ratio.From(update.TimeoutParameters.Decrease)
        );
}

public sealed record FinalizationCommitteeParametersUpdate(
    uint MinFinalizers,
    uint MaxFinalizers,
    decimal FinalizersRelativeStakeThreshold
    ) : ChainUpdatePayload
{
    internal static FinalizationCommitteeParametersUpdate From(
        Concordium.Sdk.Types.FinalizationCommitteeParametersUpdate update) =>
        new FinalizationCommitteeParametersUpdate(
            update.FinalizationCommitteeParameters.MinFinalizers,
            update.FinalizationCommitteeParameters.MaxFinalizers,
            update.FinalizationCommitteeParameters.FinalizersRelativeStakeThreshold.AsDecimal()
        );
}

public sealed record BlockEnergyLimitUpdate(
    ulong EnergyLimit) : ChainUpdatePayload
{
    internal static BlockEnergyLimitUpdate From(Concordium.Sdk.Types.BlockEnergyLimitUpdate update) => 
        new(update.EnergyLimit.Value);
}

public sealed record GasRewardsCpv2Update(
    decimal Baker,
    decimal AccountCreation,
    decimal ChainUpdate) : ChainUpdatePayload
{
    internal static GasRewardsCpv2Update From(Concordium.Sdk.Types.GasRewardsCpv2Update update) =>
        new(
            update.Baker.AsDecimal(),
            update.AccountCreation.AsDecimal(),
            update.ChainUpdate.AsDecimal()
        );
}

public record ProtocolChainUpdatePayload(
    string Message,
    string SpecificationUrl,
    string SpecificationHash,
    string SpecificationAuxiliaryDataAsHex) : ChainUpdatePayload
{
    internal static ProtocolChainUpdatePayload From(ProtocolUpdate update)
    {
        return new ProtocolChainUpdatePayload(
            update.Message,
            update.SpecificationUrl,
            update.SpecificationHash.ToHexString(),
            Convert.ToHexString(update.SpecificationAuxiliaryData).ToLowerInvariant()
        );
    }
}

public record ElectionDifficultyChainUpdatePayload(
    decimal ElectionDifficulty) : ChainUpdatePayload
{
    internal static ElectionDifficultyChainUpdatePayload From(ElectionDifficultyUpdate electionDifficultyUpdate)
    {
        return new ElectionDifficultyChainUpdatePayload(electionDifficultyUpdate.PartsPerHundredThousands.AsDecimal());
    }
}

public record EuroPerEnergyChainUpdatePayload(
    ExchangeRate ExchangeRate) : ChainUpdatePayload
{
    internal static EuroPerEnergyChainUpdatePayload From(EuroPerEnergyUpdate euroPerEnergyUpdate)
    {
        return new EuroPerEnergyChainUpdatePayload(ExchangeRate.From(euroPerEnergyUpdate.ExchangeRate));
    }
}

public record MicroCcdPerEuroChainUpdatePayload(
    ExchangeRate ExchangeRate) : ChainUpdatePayload
{
    internal static MicroCcdPerEuroChainUpdatePayload From(MicroCcdPerEuroUpdate microCcdPerEuroUpdate)
    {
        return new MicroCcdPerEuroChainUpdatePayload(ExchangeRate.From(microCcdPerEuroUpdate.ExchangeRate));
    }
}

public record FoundationAccountChainUpdatePayload(
    AccountAddress AccountAddress) : ChainUpdatePayload
{
    internal static FoundationAccountChainUpdatePayload From(FoundationAccountUpdate foundationAccountUpdate)
    {
        return new FoundationAccountChainUpdatePayload(AccountAddress.From(foundationAccountUpdate.AccountAddress));
    }
}

public record MintDistributionChainUpdatePayload(
    decimal MintPerSlot,
    decimal BakingReward,
    decimal FinalizationReward) : ChainUpdatePayload
{
    internal static MintDistributionChainUpdatePayload From(MintDistributionCpv0Update mintDistributionCpv0Update)
    {
        return new MintDistributionChainUpdatePayload(
            mintDistributionCpv0Update.MintDistribution.MintPerSlot.AsDecimal(),
            mintDistributionCpv0Update.MintDistribution.BakingReward.AsDecimal(),
            mintDistributionCpv0Update.MintDistribution.FinalizationReward.AsDecimal()
        );
    }
}

public record TransactionFeeDistributionChainUpdatePayload(
    decimal Baker,
    decimal GasAccount) : ChainUpdatePayload
{
    internal static TransactionFeeDistributionChainUpdatePayload From(TransactionFeeDistributionUpdate transactionFeeDistribution)
    {
        return new TransactionFeeDistributionChainUpdatePayload(
            transactionFeeDistribution.TransactionFeeDistribution.Baker.AsDecimal(),
            transactionFeeDistribution.TransactionFeeDistribution.GasAccount.AsDecimal());
    }
}

public record GasRewardsChainUpdatePayload(
    decimal Baker,
    decimal FinalizationProof,
    decimal AccountCreation,
    decimal ChainUpdate) : ChainUpdatePayload
{
    internal static GasRewardsChainUpdatePayload From(GasRewardsUpdate gasRewardsUpdate)
    {
        return new GasRewardsChainUpdatePayload(
            gasRewardsUpdate.GasRewards.Baker.AsDecimal(),
            gasRewardsUpdate.GasRewards.FinalizationProof.AsDecimal(),
            gasRewardsUpdate.GasRewards.AccountCreation.AsDecimal(),
            gasRewardsUpdate.GasRewards.ChainUpdate.AsDecimal()
        );
    }
}

public record BakerStakeThresholdChainUpdatePayload(
    ulong Amount) : ChainUpdatePayload
{
    internal static BakerStakeThresholdChainUpdatePayload From(BakerStakeThresholdUpdate bakerStakeThresholdUpdate)
    {
        return new BakerStakeThresholdChainUpdatePayload(bakerStakeThresholdUpdate.MinimumThresholdForBaking.Value);
    }
}

public record RootKeysChainUpdatePayload : ChainUpdatePayload
{
    [GraphQLDeprecated("Don't use! This field is only in the schema since graphql does not allow types without any fields")]
    public bool _ => false;
    
    internal static RootKeysChainUpdatePayload From(RootUpdate _)
    {
        return new RootKeysChainUpdatePayload();
    }
}

public record Level1KeysChainUpdatePayload : ChainUpdatePayload
{
    [GraphQLDeprecated("Don't use! This field is only in the schema since graphql does not allow types without any fields")]
    public bool _ => false;
    
    internal static Level1KeysChainUpdatePayload From(Level1 level1)
    {
        return new Level1KeysChainUpdatePayload();
    }
}

public record AddAnonymityRevokerChainUpdatePayload(
    int ArIdentity,
    string Name,
    string Url,
    string Description) : ChainUpdatePayload
{
    internal static AddAnonymityRevokerChainUpdatePayload From(AddAnonymityRevokerUpdate addAnonymityRevokerUpdate)
    {
        return new AddAnonymityRevokerChainUpdatePayload(
            (int)addAnonymityRevokerUpdate.ArInfo.ArIdentity.Id,
            addAnonymityRevokerUpdate.ArInfo.ArDescription.Name,
            addAnonymityRevokerUpdate.ArInfo.ArDescription.Url,
            addAnonymityRevokerUpdate.ArInfo.ArDescription.Info
        );
    }
}

public record AddIdentityProviderChainUpdatePayload(
    int IpIdentity,
    string Name,
    string Url,
    string Description) : ChainUpdatePayload
{
    internal static AddIdentityProviderChainUpdatePayload From(AddIdentityProviderUpdate addIdentityProviderUpdate)
    {
        return new AddIdentityProviderChainUpdatePayload(
            (int)addIdentityProviderUpdate.IpInfo.IpIdentity.Id,
            addIdentityProviderUpdate.IpInfo.Description.Name,
            addIdentityProviderUpdate.IpInfo.Description.Url,
            addIdentityProviderUpdate.IpInfo.Description.Info
            );
    }
}

/// <summary>
/// Cool down parameters with chain parameter version 1.
/// </summary>
/// <param name="PoolOwnerCooldown">Number of seconds that pool owners must cooldown when reducing their equity capital or closing the pool.</param>
/// <param name="DelegatorCooldown">Number of seconds that a delegator must cooldown when reducing their delegated stake.</param>
public record CooldownParametersChainUpdatePayload(
    ulong PoolOwnerCooldown,
    ulong DelegatorCooldown) : ChainUpdatePayload
{
    internal static CooldownParametersChainUpdatePayload From(CooldownParametersCpv1Update cooldownParametersCpv1Update)
    {
        return new CooldownParametersChainUpdatePayload(
            (ulong)cooldownParametersCpv1Update.CooldownParameters.PoolOwnerCooldown.TotalSeconds,
            (ulong)cooldownParametersCpv1Update.CooldownParameters.DelegatorCooldown.TotalSeconds
        );
    }
}

public record PoolParametersChainUpdatePayload(
    decimal PassiveFinalizationCommission,
    decimal PassiveBakingCommission,
    decimal PassiveTransactionCommission,
    CommissionRange FinalizationCommissionRange,
    CommissionRange BakingCommissionRange,
    CommissionRange TransactionCommissionRange,
    ulong MinimumEquityCapital,
    decimal CapitalBound,
    LeverageFactor LeverageBound) : ChainUpdatePayload
{
    internal static PoolParametersChainUpdatePayload From(PoolParametersCpv1Update poolParams)
    {
        return new PoolParametersChainUpdatePayload(
            poolParams.PoolParameters.PassiveFinalizationCommission.AsDecimal(),
            poolParams.PoolParameters.PassiveBakingCommission.AsDecimal(),
            poolParams.PoolParameters.PassiveTransactionCommission.AsDecimal(),
            CommissionRange.From(poolParams.PoolParameters.CommissionBounds.Finalization),
            CommissionRange.From(poolParams.PoolParameters.CommissionBounds.Baking),
            CommissionRange.From(poolParams.PoolParameters.CommissionBounds.Transaction),
            poolParams.PoolParameters.MinimumEquityCapital.Value,
            poolParams.PoolParameters.CapitalBound.Bound.AsDecimal(),
            LeverageFactor.From(poolParams.PoolParameters.LeverageBound)
        );
    }
}

public record TimeParametersChainUpdatePayload(
    ulong RewardPeriodLength,
    decimal MintPerPayday) : ChainUpdatePayload
{
    internal static TimeParametersChainUpdatePayload From(TimeParametersCpv1Update timeParametersCpv1Update)
    {
        return new TimeParametersChainUpdatePayload(
            timeParametersCpv1Update.TimeParameters.RewardPeriodLength.RewardPeriodEpochs.Count,
            timeParametersCpv1Update.TimeParameters.MintPrPayDay.AsDecimal()
            );
    }
}

public record MintDistributionV1ChainUpdatePayload(
    decimal BakingReward,
    decimal FinalizationReward) : ChainUpdatePayload
{
    internal static MintDistributionV1ChainUpdatePayload From(MintDistributionCpv1Update mintDistributionCpv1Update)
    {
        return new MintDistributionV1ChainUpdatePayload(
            mintDistributionCpv1Update.BakingReward.AsDecimal(),
            mintDistributionCpv1Update.FinalizationReward.AsDecimal()
        );
    }
}
    
