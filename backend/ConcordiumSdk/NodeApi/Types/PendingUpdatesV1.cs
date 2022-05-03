namespace ConcordiumSdk.NodeApi.Types;

public record PendingUpdatesV1(
    UpdateQueue<HigherLevelAccessStructureRootKeys> RootKeys,
    UpdateQueue<HigherLevelAccessStructureLevel1Keys> Level1Keys,
    UpdateQueue<AuthorizationsV1> Level2Keys,
    UpdateQueue<ProtocolUpdate> Protocol,
    UpdateQueue<decimal> ElectionDifficulty,
    UpdateQueue<ExchangeRate> EuroPerEnergy,
    UpdateQueue<ExchangeRate> MicroGTUPerEuro,
    UpdateQueue<ulong> FoundationAccount,
    UpdateQueue<MintDistributionV1> MintDistribution,
    UpdateQueue<TransactionFeeDistribution> TransactionFeeDistribution,
    UpdateQueue<GasRewards> GasRewards,
    UpdateQueue<PoolParameters> PoolParameters,
    UpdateQueue<AnonymityRevokerInfo> AddAnonymityRevoker,
    UpdateQueue<IdentityProviderInfo> AddIdentityProvider,
    UpdateQueue<CooldownParameters> CooldownParameters,
    UpdateQueue<TimeParameters> TimeParameters);