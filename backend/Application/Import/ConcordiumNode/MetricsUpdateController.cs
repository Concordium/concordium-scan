using System.Threading.Tasks;
using Application.Api.GraphQL;
using Application.Database;
using ConcordiumSdk.NodeApi.Types;
using Dapper;
using Npgsql;

namespace Application.Import.ConcordiumNode;

public class MetricsUpdateController
{
    private readonly DatabaseSettings _settings;
    private BlockInfo? _prevBlockInfo;

    public MetricsUpdateController(DatabaseSettings settings)
    {
        _settings = settings;
    }

    public async Task BlockDataReceived(BlockInfo blockInfo, BlockSummary blockSummary)
    {
        await using var conn = new NpgsqlConnection(_settings.ConnectionString);
        conn.Open();
        
        var tx = await conn.BeginTransactionAsync();

        await InsertBlockMetrics(blockInfo, conn);
        await InsertTransactionMetrics(blockInfo, blockSummary, conn);
        
        await tx.CommitAsync();
        
        _prevBlockInfo = blockInfo;
    }

    private static async Task InsertTransactionMetrics(BlockInfo blockInfo, BlockSummary blockSummary, NpgsqlConnection conn)
    {
        var transactionParams = blockSummary.TransactionSummaries.Select(txs => new
        {
            Time = blockInfo.BlockSlotTime,
            TransactionType = TransactionTypeUnion.CreateFrom(txs.Type).ToCompactString(),
            MicroCcdCost = Convert.ToInt64(txs.Cost.MicroCcdValue),
            Success = txs.Result is TransactionSuccessResult
        }).ToArray();

        var sql = "insert into metrics_transaction (time, transaction_type, micro_ccd_cost, success) values (@Time, @TransactionType, @MicroCcdCost, @Success)";
        await conn.ExecuteAsync(sql, transactionParams);
    }

    private async Task InsertBlockMetrics(BlockInfo blockInfo, NpgsqlConnection conn)
    {
        var blockParam = new
        {
            Time = blockInfo.BlockSlotTime,
            blockInfo.BlockHeight,
            BlockTimeSecs = GetBlockTime(blockInfo)
        };

        var sql = "insert into metrics_block (time, block_height, block_time_secs) values (@Time, @BlockHeight, @BlockTimeSecs)";
        await conn.ExecuteAsync(sql, blockParam);
    }

    private int GetBlockTime(BlockInfo blockInfo)
    {
        if (_prevBlockInfo == null)
            return 0; // TODO: Should only be valid for genesis block, but right now every time the app starts the first imported data item will be zero. Will be corrected later...
        var blockTime = blockInfo.BlockSlotTime - _prevBlockInfo.BlockSlotTime;
        return Convert.ToInt32(blockTime.TotalSeconds);
    }
}