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
    private long? _cumulativeAccountsCreated;

    public MetricsUpdateController(DatabaseSettings settings)
    {
        _settings = settings;
    }

    public async Task BlockDataReceived(BlockInfo blockInfo, BlockSummary blockSummary, AccountInfo[] createdAccounts)
    {
        await using var conn = new NpgsqlConnection(_settings.ConnectionString);
        conn.Open();
        
        var tx = await conn.BeginTransactionAsync();

        await InsertBlockMetrics(blockInfo, conn);
        await InsertTransactionMetrics(blockInfo, blockSummary, conn);
        await InsertAccountsMetrics(blockInfo, createdAccounts, conn);
        
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

    private async Task InsertAccountsMetrics(BlockInfo blockInfo, AccountInfo[] createdAccounts, NpgsqlConnection conn)
    {
        if (createdAccounts.Length == 0) return;

        if (!_cumulativeAccountsCreated.HasValue)
        {
            var initSql = @"select max(cumulative_accounts_created)
                            from metrics_accounts
                            where time = (select max(time) from metrics_accounts)";
            var maxTotalAccounts = await conn.QuerySingleOrDefaultAsync<long?>(initSql);
            _cumulativeAccountsCreated = maxTotalAccounts ?? 0;
        }
        
        _cumulativeAccountsCreated += createdAccounts.Length;
        
        var accountsParams = new
        {
            Time = blockInfo.BlockSlotTime,
            CumulativeAccountsCreated = _cumulativeAccountsCreated.Value,
            AccountsCreated = createdAccounts.Length
        };
        
        var sql = "insert into metrics_accounts (time, cumulative_accounts_created, accounts_created) values (@Time, @CumulativeAccountsCreated, @AccountsCreated)";
        await conn.ExecuteAsync(sql, accountsParams);
    }
}