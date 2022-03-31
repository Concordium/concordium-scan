using System.Threading.Tasks;
using Application.Api.GraphQL.EfCore;
using Application.Common.Diagnostics;
using ConcordiumSdk.NodeApi.Types;
using Microsoft.EntityFrameworkCore;

namespace Application.Api.GraphQL.Import;

public class BakerImportHandler
{
    private readonly IDbContextFactory<GraphQlDbContext> _dbContextFactory;
    private readonly IMetrics _metrics;

    public BakerImportHandler(IDbContextFactory<GraphQlDbContext> dbContextFactory, IMetrics metrics)
    {
        _dbContextFactory = dbContextFactory;
        _metrics = metrics;
    }

    public async Task AddGenesisBakers(AccountInfo[] genesisAccounts)
    {
        using var counter = _metrics.MeasureDuration(nameof(BakerImportHandler), nameof(AddGenesisBakers));

        var genesisBakers = genesisAccounts
            .Where(x => x.AccountBaker != null)
            .Select(x => x.AccountBaker!)
            .Select(x => CreateNewBaker(x.BakerId));
        
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        context.Bakers.AddRange(genesisBakers);
        await context.SaveChangesAsync();
    }

    public async Task HandleBakerUpdates(TransactionSummary[] transactions, AccountInfo[] bakersRemoved, BlockInfo blockInfo)
    {
        var bakersAdded = GetBakersAdded(transactions).ToArray();
        if (bakersAdded.Length > 0)
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync();
            context.Bakers.AddRange(bakersAdded);
            await context.SaveChangesAsync();
        }

        if (bakersRemoved.Length > 0)
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync();
            foreach (var accountInfo in bakersRemoved)
            {
                if (accountInfo.AccountBaker?.PendingChange is AccountBakerRemovePending removePending)
                {
                    var bakerId = accountInfo.AccountBaker.BakerId;
                    var baker = await context.Bakers.SingleAsync(x => x.Id == (long)bakerId);
                    
                    var eraGenesisTime = blockInfo.BlockSlotTime.AddMilliseconds(-1 * blockInfo.BlockSlot * 250);
                    var effectiveTime = eraGenesisTime.AddHours(removePending.Epoch);
                    baker.PendingBakerChange = new PendingBakerRemoval(effectiveTime);
                }
                else throw new InvalidOperationException("Account info did not have an account baker or the pending change was null!");
            }
            await context.SaveChangesAsync();
        }
    }

    private IEnumerable<Baker> GetBakersAdded(TransactionSummary[] transactions)
    {
        foreach (var successResult in transactions.Select(tx => tx.Result).OfType<TransactionSuccessResult>())
        {
            foreach (var txEvent in successResult.Events)
            {
                if (txEvent is ConcordiumSdk.NodeApi.Types.BakerAdded bakerAdded)
                    yield return CreateNewBaker(bakerAdded.BakerId);
            }
        }
    }

    private static Baker CreateNewBaker(ulong bakerId)
    {
        return new Baker
        {
            Id = (long)bakerId,
            PendingBakerChange = null,
            Status = BakerStatus.Active
        };
    }
}