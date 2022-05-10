using System.Threading.Tasks;
using Application.Api.GraphQL.Accounts;
using Application.Import;
using ConcordiumSdk.NodeApi.Types;

namespace Application.Api.GraphQL.Import;

public class DelegationImportHandler
{
    private readonly AccountWriter _writer;

    public DelegationImportHandler(AccountWriter writer)
    {
        _writer = writer;
    }

    public async Task HandleDelegationUpdates(BlockDataPayload payload, ChainParameters chainParameters)
    {
        if (payload.BlockSummary.ProtocolVersion >= 4)
        {
            var chainParametersV1 = chainParameters as ChainParametersV1 ?? throw new InvalidOperationException("Chain parameters always expect to be v1 after protocol version 4");
            
            var allTransactionEvents = payload.BlockSummary.TransactionSummaries
                .Select(tx => tx.Result).OfType<TransactionSuccessResult>()
                .SelectMany(x => x.Events)
                .ToArray();

            var txEvents = allTransactionEvents.Where(x => x
                is ConcordiumSdk.NodeApi.Types.DelegationAdded
                or ConcordiumSdk.NodeApi.Types.DelegationRemoved
                or ConcordiumSdk.NodeApi.Types.DelegationStakeDecreased
                or ConcordiumSdk.NodeApi.Types.DelegationSetRestakeEarnings);

            await UpdateDelegationFromTransactionEvents(txEvents, payload.BlockInfo, chainParametersV1);
        }
    }

    private async Task UpdateDelegationFromTransactionEvents(IEnumerable<TransactionResultEvent> txEvents,
        BlockInfo blockInfo, ChainParametersV1 chainParameters)
    {
        foreach (var txEvent in txEvents)
        {
            if (txEvent is ConcordiumSdk.NodeApi.Types.DelegationAdded added)
            {
                await _writer.UpdateAccount(added,
                    src => src.DelegatorId,
                    (_, dst) =>
                    {
                        if (dst.Delegation != null) throw new InvalidOperationException("Trying to add delegation to an account that already has a delegation set!");
                        dst.Delegation = new Delegation(false);
                    });
            }
            if (txEvent is ConcordiumSdk.NodeApi.Types.DelegationRemoved removed)
            {
                await _writer.UpdateAccount(removed,
                    src => src.DelegatorId,
                    (_, dst) =>
                    {
                        if (dst.Delegation == null) throw new InvalidOperationException("Trying to set pending change to remove delegation on an account without a delegation instance!");
                        var effectiveTime = blockInfo.BlockSlotTime.AddSeconds(chainParameters.DelegatorCooldown);
                        dst.Delegation.PendingChange = new PendingDelegationRemoval(effectiveTime);
                    });
            }
            if (txEvent is ConcordiumSdk.NodeApi.Types.DelegationStakeDecreased stakeDecreased)
            {
                await _writer.UpdateAccount(stakeDecreased,
                    src => src.DelegatorId,
                    (src, dst) =>
                    {
                        if (dst.Delegation == null) throw new InvalidOperationException("Trying to set pending change to remove delegation on an account without a delegation instance!");
                        var effectiveTime = blockInfo.BlockSlotTime.AddSeconds(chainParameters.DelegatorCooldown);
                        dst.Delegation.PendingChange = new PendingDelegationReduceStake(effectiveTime, stakeDecreased.NewStake.MicroCcdValue);
                    });
            }
            else if (txEvent is ConcordiumSdk.NodeApi.Types.DelegationSetRestakeEarnings setRestakeEarnings)
            {
                await _writer.UpdateAccount(setRestakeEarnings,
                    src => src.DelegatorId,
                    (src, dst) =>
                    {
                        if (dst.Delegation == null) throw new InvalidOperationException("Trying to set restake earnings flag on an account without a delegation instance!");
                        dst.Delegation.RestakeEarnings = src.RestakeEarnings;
                    });
            }
        }
    }
}