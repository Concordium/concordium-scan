using System.Threading.Tasks;
using HotChocolate.Types;
using HotChocolate.Types.Pagination;

namespace Application.Api.GraphQL;

public class Query
{
    private readonly SampleDataSet _sampleDataSet;
    private const int DefaultPageSize = 20;
    
    public Query(SampleDataSet sampleDataSet)
    {
        _sampleDataSet = sampleDataSet;
    }

    [UsePaging(MaxPageSize = 50, DefaultPageSize = DefaultPageSize)]
    public Connection<Block> GetBlocks(string? after, int? first, string? before, int? last)
    {
        int? afterId = after != null ? Convert.ToInt32(after) : null;
        int? beforeId = before != null ? Convert.ToInt32(before) : null;

        var blocks = FindBlocks(afterId, beforeId, first, last);
        
        var edges = blocks
            .Select(block => new Edge<Block>(block, block.Id.ToString()))
            .ToArray();

        var pageInfo = new ConnectionPageInfo(
            !ReferenceEquals(blocks.Last(), _sampleDataSet.AllBlocks.Last()), 
            !ReferenceEquals(blocks.First(), _sampleDataSet.AllBlocks.First()), 
            blocks.First().Id.ToString(), 
            blocks.Last().Id.ToString());

        return new Connection<Block>(edges, pageInfo, ct => ValueTask.FromResult(0));
    }

    [UsePaging(MaxPageSize = 50, DefaultPageSize = DefaultPageSize)]
    public Connection<Transaction> GetTransactions(string? after, int? first, string? before, int? last)
    {
        int? afterId = after != null ? Convert.ToInt32(after) : null;
        int? beforeId = before != null ? Convert.ToInt32(before) : null;

        var transactions = FindTransactions(afterId, beforeId, first, last);
        
        var edges = transactions
            .Select(transaction => new Edge<Transaction>(transaction, transaction.Id.ToString()))
            .ToArray();

        var pageInfo = transactions.Any()
            ? new ConnectionPageInfo(
                !ReferenceEquals(transactions.Last(), _sampleDataSet.AllTransactions.Last()),
                !ReferenceEquals(transactions.First(), _sampleDataSet.AllTransactions.First()),
                transactions.First().Id.ToString(),
                transactions.Last().Id.ToString())
            : new ConnectionPageInfo(false, false, null, null);

        return new Connection<Transaction>(edges, pageInfo, ct => ValueTask.FromResult(0));
    }

    private Block[] FindBlocks(int? afterId, int? beforeId, int? first, int? last)
    {
        var allBlocks = _sampleDataSet.AllBlocks;
        if (afterId.HasValue)
            return allBlocks.Where(x => x.BlockHeight > afterId.Value).Take(first ?? DefaultPageSize).ToArray();
        if (beforeId.HasValue)
            return allBlocks.Where(x => x.BlockHeight < beforeId.Value).TakeLast(last ?? DefaultPageSize).ToArray();
        if (last.HasValue)
            return allBlocks.TakeLast(last.Value).ToArray();
        return allBlocks.Take(first ?? DefaultPageSize).ToArray();
    }

    private Transaction[] FindTransactions(int? afterId, int? beforeId, int? first, int? last)
    {
        var allTransactions = _sampleDataSet.AllTransactions;
        if (afterId.HasValue)
            return allTransactions.Where(x => x.Id > afterId.Value).Take(first ?? DefaultPageSize).ToArray();
        if (beforeId.HasValue)
            return allTransactions.Where(x => x.Id < beforeId.Value).TakeLast(last ?? DefaultPageSize).ToArray();
        if (last.HasValue)
            return allTransactions.TakeLast(last.Value).ToArray();
        return allTransactions.Take(first ?? DefaultPageSize).ToArray();
    }

    
    
}
