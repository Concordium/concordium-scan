using Application.Database;
using Application.Import.ConcordiumNode.GrpcClient;
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

    public void Insert(BlockInfo blockInfo, string blockSummary)
    {
        using var conn = new NpgsqlConnection(_settings.ConnectionString);
        conn.Open();

        var blockParams = new
        {
            Blockhash = blockInfo.BlockHash.AsBytes,
            Parentblock = blockInfo.BlockParent.AsBytes,
            Blocklastfinalized = blockInfo.BlockLastFinalized.AsBytes,
            Blockheight = blockInfo.BlockHeight,
            Genesisindex = blockInfo.GenesisIndex,
            Erablockheight = blockInfo.EraBlockHeight,
            Blockreceivetime = blockInfo.BlockReceiveTime,
            Blockarrivetime = blockInfo.BlockArriveTime,
            Blockslot = blockInfo.BlockSlot,
            Blockslottime = blockInfo.BlockSlotTime,
            Blockbaker = blockInfo.BlockBaker,
            Finalized = blockInfo.Finalized,
            Transactioncount = blockInfo.TransactionCount,
            Transactionenergycost = blockInfo.TransactionEnergyCost,
            Transactionsize = blockInfo.TransactionSize,
            Blockstatehash = new BlockHash(blockInfo.BlockStateHash).AsBytes,
            Blocksummary = blockSummary
        };

        conn.Execute(
            "INSERT INTO block(blockhash, parentblock, blocklastfinalized, blockheight, genesisindex, erablockheight, blockreceivetime, blockarrivetime, blockslot, blockslottime, blockbaker, finalized, transactioncount, transactionenergycost, transactionsize, blockstatehash, blocksummary) VALUES (@Blockhash, @Parentblock, @Blocklastfinalized, @Blockheight, @Genesisindex,  @Erablockheight, @Blockreceivetime, @Blockarrivetime, @Blockslot, @Blockslottime, @Blockbaker, @Finalized, @Transactioncount, @Transactionenergycost, @Transactionsize, @Blockstatehash, CAST(@Blocksummary AS json))",
            blockParams);
    }

    public int? GetMaxBlockHeight()
    {
        using var conn = new NpgsqlConnection(_settings.ConnectionString);
        conn.Open();

        return conn.QuerySingle<int?>("SELECT max(blockheight) FROM block");
    }
}