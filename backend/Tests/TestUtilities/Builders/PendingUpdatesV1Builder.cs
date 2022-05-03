using ConcordiumSdk.NodeApi.Types;

namespace Tests.TestUtilities.Builders;

public class PendingUpdatesV1Builder
{
    public PendingUpdatesV1 Build()
    {
        return new PendingUpdatesV1(
            new UpdateQueue<HigherLevelAccessStructureRootKeys>(0, Array.Empty<ScheduledUpdate<HigherLevelAccessStructureRootKeys>>()),
            new UpdateQueue<HigherLevelAccessStructureLevel1Keys>(0, Array.Empty<ScheduledUpdate<HigherLevelAccessStructureLevel1Keys>>()),
            new UpdateQueue<AuthorizationsV1>(0, Array.Empty<ScheduledUpdate<AuthorizationsV1>>()),
            new UpdateQueue<ProtocolUpdate>(0, Array.Empty<ScheduledUpdate<ProtocolUpdate>>()),
            new UpdateQueue<decimal>(0, Array.Empty<ScheduledUpdate<decimal>>()),
            new UpdateQueue<ExchangeRate>(0, Array.Empty<ScheduledUpdate<ExchangeRate>>()),
            new UpdateQueue<ExchangeRate>(0, Array.Empty<ScheduledUpdate<ExchangeRate>>()),
            new UpdateQueue<ulong>(0, Array.Empty<ScheduledUpdate<ulong>>()),
            new UpdateQueue<MintDistributionV1>(0, Array.Empty<ScheduledUpdate<MintDistributionV1>>()),
            new UpdateQueue<TransactionFeeDistribution>(0, Array.Empty<ScheduledUpdate<TransactionFeeDistribution>>()),
            new UpdateQueue<GasRewards>(0, Array.Empty<ScheduledUpdate<GasRewards>>()),
            new UpdateQueue<PoolParameters>(0, Array.Empty<ScheduledUpdate<PoolParameters>>()),
            new UpdateQueue<AnonymityRevokerInfo>(0, Array.Empty<ScheduledUpdate<AnonymityRevokerInfo>>()),
            new UpdateQueue<IdentityProviderInfo>(0, Array.Empty<ScheduledUpdate<IdentityProviderInfo>>()),
            new UpdateQueue<CooldownParameters>(0, Array.Empty<ScheduledUpdate<CooldownParameters>>()),
            new UpdateQueue<TimeParameters>(0, Array.Empty<ScheduledUpdate<TimeParameters>>())
        );
    }
}