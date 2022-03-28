﻿using HotChocolate;
using HotChocolate.Types;

namespace Application.Api.GraphQL;

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

public record RootKeysChainUpdatePayload(
    [property:GraphQLDeprecated("Don't use! This field is only in the schema since graphql does not allow types without any fields")]
    bool _ = false) : ChainUpdatePayload;

public record Level1KeysChainUpdatePayload(
    [property:GraphQLDeprecated("Don't use! This field is only in the schema since graphql does not allow types without any fields")]
    bool _ = false) : ChainUpdatePayload;

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