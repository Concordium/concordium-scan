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

    public async Task HandleDelegationUpdates(BlockDataPayload payload)
    {
        var allTransactionEvents = payload.BlockSummary.TransactionSummaries
            .Select(tx => tx.Result).OfType<TransactionSuccessResult>()
            .SelectMany(x => x.Events)
            .ToArray();

        var txEvents = allTransactionEvents.Where(x => x
            is ConcordiumSdk.NodeApi.Types.DelegationAdded
            or ConcordiumSdk.NodeApi.Types.DelegationSetRestakeEarnings);

        await UpdateDelegationFromTransactionEvents(txEvents);
    }

    private async Task UpdateDelegationFromTransactionEvents(IEnumerable<TransactionResultEvent> txEvents)
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