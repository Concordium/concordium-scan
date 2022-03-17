using ConcordiumSdk.NodeApi.Types;
using ConcordiumSdk.Types;

namespace Tests.TestUtilities.Builders;

public class PendingUpdatesBuilder
{
    public PendingUpdates Build()
    {
        return new PendingUpdates(
            new UpdateQueue<HigherLevelAccessStructureRootKeys>(0, Array.Empty<ScheduledUpdate<HigherLevelAccessStructureRootKeys>>()),
            new UpdateQueue<HigherLevelAccessStructureLevel1Keys>(0, Array.Empty<ScheduledUpdate<HigherLevelAccessStructureLevel1Keys>>()),
            new UpdateQueue<Authorizations>(0, Array.Empty<ScheduledUpdate<Authorizations>>()),
            new UpdateQueue<ProtocolUpdate>(0, Array.Empty<ScheduledUpdate<ProtocolUpdate>>()),
            new UpdateQueue<decimal>(0, Array.Empty<ScheduledUpdate<decimal>>()),
            new UpdateQueue<ExchangeRate>(0, Array.Empty<ScheduledUpdate<ExchangeRate>>()),
            new UpdateQueue<ExchangeRate>(0, Array.Empty<ScheduledUpdate<ExchangeRate>>()),
            new UpdateQueue<ulong>(0, Array.Empty<ScheduledUpdate<ulong>>()),
            new UpdateQueue<MintDistribution>(0, Array.Empty<ScheduledUpdate<MintDistribution>>()),
            new UpdateQueue<TransactionFeeDistribution>(0, Array.Empty<ScheduledUpdate<TransactionFeeDistribution>>()),
            new UpdateQueue<GasRewards>(0, Array.Empty<ScheduledUpdate<GasRewards>>()),
            new UpdateQueue<CcdAmount>(0, Array.Empty<ScheduledUpdate<CcdAmount>>()),
            new UpdateQueue<AnonymityRevokerInfo>(0, Array.Empty<ScheduledUpdate<AnonymityRevokerInfo>>()),
            new UpdateQueue<IdentityProviderInfo>(0, Array.Empty<ScheduledUpdate<IdentityProviderInfo>>())
        );
    }
}