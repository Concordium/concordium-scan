using System.Threading.Tasks;
using Application.Database;
using ConcordiumSdk.Types;
using Dapper;
using HotChocolate.Types;
using HotChocolate.Types.Pagination;
using Npgsql;

namespace Application.Api.GraphQL;

public class Query
{
    private const int DefaultPageSize = 20;
    private readonly DatabaseSettings _dbSettings;
    private readonly Lazy<Block[]> _allBlocks; 
    private readonly Lazy<Transaction[]> _allTransactions; 
    
    public Query(DatabaseSettings dbSettings)
    {
        _dbSettings = dbSettings;
        _allBlocks = new Lazy<Block[]>(FetchSampleBlockSetFromDb);
        _allTransactions = new Lazy<Transaction[]>(FetchSampleTransactionSetFromDb);
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
            !ReferenceEquals(blocks.Last(), _allBlocks.Value.Last()), 
            !ReferenceEquals(blocks.First(), _allBlocks.Value.First()), 
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
                !ReferenceEquals(transactions.Last(), _allTransactions.Value.Last()),
                !ReferenceEquals(transactions.First(), _allTransactions.Value.First()),
                transactions.First().Id.ToString(),
                transactions.Last().Id.ToString())
            : new ConnectionPageInfo(false, false, null, null);

        return new Connection<Transaction>(edges, pageInfo, ct => ValueTask.FromResult(0));
    }

    private Block[] FindBlocks(int? afterId, int? beforeId, int? first, int? last)
    {
        if (afterId.HasValue)
            return _allBlocks.Value.Where(x => x.BlockHeight > afterId.Value).Take(first ?? DefaultPageSize).ToArray();
        if (beforeId.HasValue)
            return _allBlocks.Value.Where(x => x.BlockHeight < beforeId.Value).TakeLast(last ?? DefaultPageSize).ToArray();
        if (last.HasValue)
            return _allBlocks.Value.TakeLast(last.Value).ToArray();
        return _allBlocks.Value.Take(first ?? DefaultPageSize).ToArray();
    }

    private Transaction[] FindTransactions(int? afterId, int? beforeId, int? first, int? last)
    {
        if (afterId.HasValue)
            return _allTransactions.Value.Where(x => x.Id > afterId.Value).Take(first ?? DefaultPageSize).ToArray();
        if (beforeId.HasValue)
            return _allTransactions.Value.Where(x => x.Id < beforeId.Value).TakeLast(last ?? DefaultPageSize).ToArray();
        if (last.HasValue)
            return _allTransactions.Value.TakeLast(last.Value).ToArray();
        return _allTransactions.Value.Take(first ?? DefaultPageSize).ToArray();
    }

    private Block[] FetchSampleBlockSetFromDb()
    {
        using var conn = new NpgsqlConnection(_dbSettings.ConnectionString);
        conn.Open();

        var result =
            conn.Query(
                "SELECT id, block_hash, block_height, block_slot_time, transaction_count FROM block WHERE block_height < 40000");
        
        return result.Select(obj => new Block()
        {
            Id = obj.id,
            BlockHash = new BlockHash((byte[])obj.block_hash).AsString,
            BlockHeight = (int)obj.block_height,
            BlockSlotTime = (DateTimeOffset)obj.block_slot_time,
            Finalized = true,
            TransactionCount = (int)obj.transaction_count
        }).ToArray();
    }
    
    private Transaction[] FetchSampleTransactionSetFromDb()
    {
        using var conn = new NpgsqlConnection(_dbSettings.ConnectionString);
        conn.Open();

        var result =
            conn.Query(
                "SELECT id, block_height, block_hash, transaction_index, transaction_hash, sender, cost, energy_cost FROM transaction_summary WHERE block_height < 40000");
        
        return result.Select(obj => new Transaction()
        {
            Id = obj.id,
            BlockHeight = (int)obj.block_height,
            BlockHash = new BlockHash((byte[])obj.block_hash).AsString,
            TransactionIndex = obj.transaction_index,
            TransactionHash = new TransactionHash((byte[])obj.transaction_hash).AsString,
            SenderAccountAddress = obj.sender != null ? new AccountAddress((byte[])obj.sender).AsString : "",
            CcdCost = obj.cost,
            EnergyCost = obj.energy_cost
        }).ToArray();
    }
    
    
}
