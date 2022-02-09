using System.Data;
using System.Text.Json;
using System.Threading.Tasks;
using Application.Database;
using ConcordiumSdk.NodeApi;
using ConcordiumSdk.NodeApi.Types;
using ConcordiumSdk.Types;
using Dapper;
using Npgsql;

namespace Application.Persistence;

public class BlockRepository
{
    private readonly DatabaseSettings _settings;

    public BlockRepository(DatabaseSettings settings)
    {
        _settings = settings;
    }

    public int? GetMaxBlockHeight()
    {
        using var conn = new NpgsqlConnection(_settings.ConnectionString);
        conn.Open();

        var data = conn.QuerySingleOrDefault("SELECT block_height FROM block order by id desc limit 1");
        if (data == null) return null; 
        return (int)data.block_height;
    }

    public BlockHash? GetGenesisBlockHash()
    {
        using var conn = new NpgsqlConnection(_settings.ConnectionString);
        conn.Open();

        var data = conn.QuerySingleOrDefault("SELECT block_height, block_hash FROM block order by id limit 1");
        if (data == null) return null;
        if (data.block_height != 0) throw new InvalidOperationException("Did not get the genesis block - unexpected!");
        var result = data != null ? new BlockHash((byte[])data.block_hash) : null;
        return result;
    }

    public async Task Insert(BlockInfo blockInfo, string blockSummaryString, BlockSummary blockSummary)
    {
        await using var conn = new NpgsqlConnection(_settings.ConnectionString);
        await conn.OpenAsync();

        await using var tx = await conn.BeginTransactionAsync(IsolationLevel.ReadCommitted);

        var blockParams = new
        {
            Blockhash = blockInfo.BlockHash.AsBytes,
            Blockheight = blockInfo.BlockHeight,
            Blocksummary = blockSummaryString,
        };
        
        await conn.ExecuteScalarAsync<long>(
            "INSERT INTO block(block_height, block_hash, block_summary) " +
            " VALUES (@Blockheight, @Blockhash, CAST(@Blocksummary AS json))",
            blockParams);

        await tx.CommitAsync();
    }
}