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
    
    public Query(DatabaseSettings dbSettings)
    {
        _dbSettings = dbSettings;
        _allBlocks = new Lazy<Block[]>(FetchSampleSetFromDb);
    }

    [UsePaging(MaxPageSize = 50, DefaultPageSize = DefaultPageSize)]
    public Connection<Block> GetBlocks(string? after, int? first, string? before, int? last)
    {
        int? afterId = after != null ? Convert.ToInt32(after) : null;
        int? beforeId = before != null ? Convert.ToInt32(before) : null;

        var blocks = FindBlocks(afterId, beforeId, first, last);
        
        var edges = blocks
            .Select(block => new Edge<Block>(block, block.BlockHeight.ToString()))
            .ToArray();

        var pageInfo = new ConnectionPageInfo(!ReferenceEquals(blocks.Last(), _allBlocks.Value.Last()), !ReferenceEquals(blocks.First(), _allBlocks.Value.First()), blocks.First().BlockHeight.ToString(), blocks.Last().BlockHeight.ToString());

        return new Connection<Block>(edges, pageInfo, ct => ValueTask.FromResult(0));
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

    private Block[] FetchSampleSetFromDb()
    {
        using var conn = new NpgsqlConnection(_dbSettings.ConnectionString);
        conn.Open();

        var result =
            conn.Query(
                "SELECT block_hash, block_height, block_slot_time, transaction_count FROM finalized_block WHERE block_height < 500");
        
        return result.Select(obj => new Block()
        {
            BlockHash = new BlockHash((byte[])obj.block_hash).AsString,
            BlockHeight = (int)obj.block_height,
            BlockSlotTime = (DateTimeOffset)obj.block_slot_time,
            Finalized = true,
            TransactionCount = (int)obj.transaction_count
        }).ToArray();
    }
}
