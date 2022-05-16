using ConcordiumSdk.Types;

namespace ConcordiumSdk.NodeApi.Types;

public abstract record UpdatePayload;

public record ProtocolUpdatePayload(
    ProtocolUpdate Content) : UpdatePayload;

public record ElectionDifficultyUpdatePayload(
    decimal ElectionDifficulty) : UpdatePayload;

public record EuroPerEnergyUpdatePayload(
    ExchangeRate Content) : UpdatePayload;

public record MicroGtuPerEuroUpdatePayload(
    ExchangeRate Content) : UpdatePayload;

public record FoundationAccountUpdatePayload(
    AccountAddress Account) : UpdatePayload;

public record MintDistributionV0UpdatePayload(
    MintDistributionV0 Content) : UpdatePayload;

public record TransactionFeeDistributionUpdatePayload(
    TransactionFeeDistribution Content) : UpdatePayload;

public record GasRewardsUpdatePayload(
    GasRewards Content) : UpdatePayload;

public record BakerStakeThresholdUpdatePayload(
    CcdAmount Amount) : UpdatePayload;

public record RootUpdatePayload(
     RootUpdate Content) : UpdatePayload;

public record Level1UpdatePayload(
     Level1Update Content) : UpdatePayload;

public record AddAnonymityRevokerUpdatePayload(
    AnonymityRevokerInfo Content) : UpdatePayload;

public record AddIdentityProviderUpdatePayload(
    IdentityProviderInfo Content) : UpdatePayload;

public record CooldownParametersUpdatePayload(
    CooldownParameters Content) : UpdatePayload;
    
public record PoolParametersUpdatePayload(
    PoolParameters Content) : UpdatePayload;
    
public record TimeParametersUpdatePayload(
    TimeParameters Content) : UpdatePayload;
    
public record MintDistributionV1UpdatePayload(
    MintDistributionV1 Content) : UpdatePayload;
    
    