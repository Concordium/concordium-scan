using System.Threading.Tasks;
using Application.Api.GraphQL.EfCore;
using Application.Api.GraphQL.Transactions;
using Application.Common.Diagnostics;
using Concordium.Sdk.Types;
using Microsoft.EntityFrameworkCore;
using TransactionResultEvent = Application.Api.GraphQL.Transactions.TransactionResultEvent;

namespace Application.Api.GraphQL.Import;

public class TransactionWriter
{
    private readonly IDbContextFactory<GraphQlDbContext> _dbContextFactory;
    private readonly IMetrics _metrics;

    public TransactionWriter(IDbContextFactory<GraphQlDbContext> dbContextFactory, IMetrics metrics)
    {
        _dbContextFactory = dbContextFactory;
        _metrics = metrics;
    }

    public async Task<TransactionPair[]> AddTransactions(IList<BlockItemSummary> blockItemSummaries, long blockId, DateTimeOffset blockSlotTime)
    {
        if (blockItemSummaries.Count == 0) return Array.Empty<TransactionPair>();
        
        using var counter = _metrics.MeasureDuration(nameof(TransactionWriter), nameof(AddTransactions));

        await using var context = await _dbContextFactory.CreateDbContextAsync();

        var transactions = blockItemSummaries
            .Select(x => new TransactionPair(x,  Transaction.MapTransaction(x, blockId)))
            .ToArray();
        context.Transactions.AddRange(transactions.Select(x => x.Target));
        await context.SaveChangesAsync(); // assigns transaction ids

        foreach (var transaction in transactions)
        {
            if (!transaction.Source.IsSuccess())
            {
                continue;
            }

            var events = TransactionResultEvent.ToIter(transaction.Source.Details, blockSlotTime)
                .Select((x, ix) => 
                    new TransactionRelated<TransactionResultEvent>(transaction.Target.Id, ix, x))
                .ToArray();
            
            context.TransactionResultEvents.AddRange(events);
        }
        await context.SaveChangesAsync();
        return transactions;
    }

}