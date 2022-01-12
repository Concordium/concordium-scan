using ConcordiumSdk.Types;

namespace ConcordiumSdk.NodeApi.Types;

public record PendingUpdates(
    UpdateQueue<HigherLevelAccessStructureRootKeys> RootKeys,
    UpdateQueue<HigherLevelAccessStructureLevel1Keys> Level1Keys,
    UpdateQueue<Authorizations> Level2Keys,
    UpdateQueue<ProtocolUpdate> Protocol,
    UpdateQueue<decimal> ElectionDifficulty,
    UpdateQueue<ExchangeRate> EuroPerEnergy,
    UpdateQueue<ExchangeRate> MicroGTUPerEuro,
    UpdateQueue<ulong> FoundationAccount,
    UpdateQueue<MintDistribution> MintDistribution,
    UpdateQueue<TransactionFeeDistribution> TransactionFeeDistribution,
    UpdateQueue<GasRewards> GasRewards,
    UpdateQueue<CcdAmount> BakerStakeThreshold,
    UpdateQueue<AnonymityRevokerInfo> AddAnonymityRevoker,
    UpdateQueue<IdentityProviderInfo> AddIdentityProvider);