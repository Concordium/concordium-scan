using ConcordiumSdk.Types;

namespace ConcordiumSdk.NodeApi.Types;

public record PendingUpdatesV0(
    UpdateQueue<HigherLevelAccessStructureRootKeys> RootKeys,
    UpdateQueue<HigherLevelAccessStructureLevel1Keys> Level1Keys,
    UpdateQueue<AuthorizationsV0> Level2Keys,
    UpdateQueue<ProtocolUpdate> Protocol,
    UpdateQueue<decimal> ElectionDifficulty,
    UpdateQueue<ExchangeRate> EuroPerEnergy,
    UpdateQueue<ExchangeRate> MicroGTUPerEuro,
    UpdateQueue<ulong> FoundationAccount,
    UpdateQueue<MintDistributionV0> MintDistribution,
    UpdateQueue<TransactionFeeDistribution> TransactionFeeDistribution,
    UpdateQueue<GasRewards> GasRewards,
    UpdateQueue<CcdAmount> BakerStakeThreshold,
    UpdateQueue<AnonymityRevokerInfo> AddAnonymityRevoker,
    UpdateQueue<IdentityProviderInfo> AddIdentityProvider);