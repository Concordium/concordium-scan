using System.Threading.Tasks;
using Application.Database;
using Application.Import;
using ConcordiumSdk.Types;
using Dapper;
using Npgsql;

namespace Application.Api.GraphQL.EfCore;

public class ImportStateReader
{
    private readonly DatabaseSettings _settings;

    public ImportStateReader(DatabaseSettings settings)
    {
        _settings = settings;
    }

    public async Task<ImportState> ReadImportStatus()
    {
        var maxBlockHeight = await ReadMaxBlockHeight();
        var genesisBlockHash = await ReadGenesisBlockHash();
        return new ImportState(maxBlockHeight, genesisBlockHash);
    }

    private async Task<long?> ReadMaxBlockHeight()
    {
        await using var conn = new NpgsqlConnection(_settings.ConnectionString);
        await conn.OpenAsync();

        var data = await conn.QuerySingleOrDefaultAsync("SELECT block_height FROM graphql_blocks order by block_height desc limit 1");
        if (data == null) return null; 
        return (long)data.block_height;
    }

    private async Task<BlockHash?> ReadGenesisBlockHash()
    {
        await using var conn = new NpgsqlConnection(_settings.ConnectionString);
        await conn.OpenAsync();

        var data = await conn.QuerySingleOrDefaultAsync("SELECT block_hash, block_height FROM graphql_blocks order by block_height limit 1");
        if (data == null) return null;
        var blockHeight = (long)data.block_height;
        if (blockHeight != 0) throw new InvalidOperationException("Block with lowest block height is not at height zero!");
        var result = new BlockHash((string)data.block_hash);
        return result;
    }
}

