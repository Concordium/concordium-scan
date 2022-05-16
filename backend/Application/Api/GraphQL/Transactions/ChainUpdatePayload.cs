using Application.Api.GraphQL.Accounts;
using HotChocolate;
using HotChocolate.Types;

namespace Application.Api.GraphQL.Transactions;

[UnionType]
public abstract record ChainUpdatePayload;

public record ProtocolChainUpdatePayload(
    string Message,
    string SpecificationUrl,
    string SpecificationHash,
    string SpecificationAuxiliaryDataAsHex) : ChainUpdatePayload;

public record ElectionDifficultyChainUpdatePayload(
    decimal ElectionDifficulty) : ChainUpdatePayload;

public record EuroPerEnergyChainUpdatePayload(
    ExchangeRate ExchangeRate) : ChainUpdatePayload;

public record MicroCcdPerEuroChainUpdatePayload(
    ExchangeRate ExchangeRate) : ChainUpdatePayload;

public record FoundationAccountChainUpdatePayload(
    AccountAddress AccountAddress) : ChainUpdatePayload;

public record MintDistributionChainUpdatePayload(
    decimal MintPerSlot,
    decimal BakingReward,
    decimal FinalizationReward) : ChainUpdatePayload;

public record TransactionFeeDistributionChainUpdatePayload(
    decimal Baker,
    decimal GasAccount) : ChainUpdatePayload;

public record GasRewardsChainUpdatePayload(
    decimal Baker,
    decimal FinalizationProof,
    decimal AccountCreation,
    decimal ChainUpdate) : ChainUpdatePayload;

public record BakerStakeThresholdChainUpdatePayload(
    ulong Amount) : ChainUpdatePayload;

public record RootKeysChainUpdatePayload : ChainUpdatePayload
{
    [GraphQLDeprecated("Don't use! This field is only in the schema since graphql does not allow types without any fields")]
    public bool _ => false;
}

public record Level1KeysChainUpdatePayload : ChainUpdatePayload
{
    [GraphQLDeprecated("Don't use! This field is only in the schema since graphql does not allow types without any fields")]
    public bool _ => false;
}

public record AddAnonymityRevokerChainUpdatePayload(
    int ArIdentity, 
    string Name,
    string Url, 
    string Description) : ChainUpdatePayload;

public record AddIdentityProviderChainUpdatePayload(
    int IpIdentity, 
    string Name,
    string Url, 
    string Description) : ChainUpdatePayload;

public record CooldownParametersChainUpdatePayload(
    ulong PoolOwnerCooldown,
    ulong DelegatorCooldown) : ChainUpdatePayload;

public record PoolParametersChainUpdatePayload(
    decimal PassiveFinalizationCommission,
    decimal PassiveBakingCommission,
    decimal PassiveTransactionCommission,
    CommissionRange FinalizationCommissionRange,
    CommissionRange BakingCommissionRange,
    CommissionRange TransactionCommissionRange,
    ulong MinimumEquityCapital,
    decimal CapitalBound,
    LeverageFactor LeverageBound) : ChainUpdatePayload;

public record TimeParametersChainUpdatePayload(
    ulong RewardPeriodLength,
    decimal MintPerPayday) : ChainUpdatePayload;

public record MintDistributionV1ChainUpdatePayload(
    decimal BakingReward,
    decimal FinalizationReward) : ChainUpdatePayload;
    