using System.Threading.Tasks;
using Application.Api.GraphQL.Bakers;
using Application.Api.GraphQL.EfCore;
using Application.Common.Diagnostics;
using ConcordiumSdk.NodeApi.Types;
using Microsoft.EntityFrameworkCore;

namespace Application.Api.GraphQL.Import;

public class BakerImportHandler
{
    private readonly BakerWriter _writer;
    private readonly IMetrics _metrics;
    private readonly ILogger _logger;

    public BakerImportHandler(IDbContextFactory<GraphQlDbContext> dbContextFactory, IMetrics metrics)
    {
        _writer = new BakerWriter(dbContextFactory, metrics);
        _metrics = metrics;
        _logger = Log.ForContext(GetType());
    }

    public async Task AddGenesisBakers(AccountInfo[] genesisAccounts)
    {
        using var counter = _metrics.MeasureDuration(nameof(BakerImportHandler), nameof(AddGenesisBakers));

        var genesisBakers = genesisAccounts
            .Where(x => x.AccountBaker != null)
            .Select(x => x.AccountBaker!)
            .Select(x => CreateNewBaker(x.BakerId, x.RestakeEarnings));

        await _writer.AddBakers(genesisBakers);
    }

    public async Task HandleBakerUpdates(TransactionSummary[] transactions, AccountInfo[] bakersRemoved, BlockInfo blockInfo, ImportState importState)
    {
        using var counter = _metrics.MeasureDuration(nameof(BakerImportHandler), nameof(HandleBakerUpdates));

        var txEventsOrdered = transactions
            .Select(tx => tx.Result).OfType<TransactionSuccessResult>()
            .SelectMany(x => x.Events);
        
        foreach (var txEvent in txEventsOrdered)
        {
            if (txEvent is ConcordiumSdk.NodeApi.Types.BakerAdded bakerAdded)
            {
                await _writer.AddOrUpdateBaker(bakerAdded,
                    src => src.BakerId,
                    src => CreateNewBaker(src.BakerId, src.RestakeEarnings),
                    (src, dst) => { dst.State = new ActiveBakerState(src.RestakeEarnings, null); });
            }

            if (txEvent is ConcordiumSdk.NodeApi.Types.BakerSetRestakeEarnings restakeEarnings)
            {
                await _writer.UpdateBaker(restakeEarnings,
                    src => src.BakerId,
                    (src, dst) =>
                    {
                        var activeState = dst.State as ActiveBakerState ?? throw new InvalidOperationException("Cannot set restake earnings for a baker that is not active!");
                        activeState.RestakeEarnings = src.RestakeEarnings;
                    });
            }
        }
        
        if (bakersRemoved.Length > 0)
        {
            var source = bakersRemoved.Select(x => x.AccountBaker!).ToArray();
            var updatedBakers = await _writer.UpdateBakersFromAccountBaker(source, (dst, src) => SetPendingChange(dst, src, blockInfo));

            var minEffectiveTime = updatedBakers.Min(x => ((ActiveBakerState)x.State).PendingChange!.EffectiveTime);
            if (!importState.NextPendingBakerChangeTime.HasValue || importState.NextPendingBakerChangeTime.Value > minEffectiveTime)
                importState.NextPendingBakerChangeTime = minEffectiveTime;
        }

        if (blockInfo.BlockSlotTime > importState.NextPendingBakerChangeTime)
        {
            await _writer.UpdateBakersWithPendingChange(blockInfo.BlockSlotTime, ApplyPendingChange);

            importState.NextPendingBakerChangeTime = await _writer.GetMinPendingChangeTime();
            _logger.Information("NextPendingBakerChangeTime set to {value}", importState.NextPendingBakerChangeTime);
        }
    }

    private void SetPendingChange(Baker destination, AccountBaker source, BlockInfo blockInfo)
    {
        if (source.PendingChange is AccountBakerRemovePending removePending)
        {
            // TODO: Prior to protocol update 4, the effective time must be calculated in this cumbersome way
            //       We should be able to change this once we switch to concordium node v4 or greater!
            //
            // BUILT-IN ASSUMPTIONS (that can change but probably wont):
            //       Block time is 250ms
            //       Epoch duration is 1 hour
            var eraGenesisTime = blockInfo.BlockSlotTime.AddMilliseconds(-1 * blockInfo.BlockSlot * 250);
            var effectiveTime = eraGenesisTime.AddHours(removePending.Epoch);

            var activeState = destination.State as ActiveBakerState ?? throw new InvalidOperationException("Pending baker removal for a baker that was not active!");
            activeState.PendingChange = new PendingBakerRemoval(effectiveTime);
        }
    }

    private void ApplyPendingChange(Baker baker)
    {
        var activeState = baker.State as ActiveBakerState ?? throw new InvalidOperationException("Applying pending change to a baker that was not active!");
        if (activeState.PendingChange is PendingBakerRemoval pendingRemoval)
        {
            _logger.Information("Baker with id {bakerId} will be removed.", baker.Id);

            baker.State = new RemovedBakerState(pendingRemoval.EffectiveTime);
        }
    }

    private static Baker CreateNewBaker(ulong bakerId, bool restakeEarnings)
    {
        return new Baker
        {
            Id = (long)bakerId,
            State = new ActiveBakerState(restakeEarnings, null)
        };
    }
}