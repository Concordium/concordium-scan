using System.Threading.Tasks;
using Application.Common;
using Application.Database;
using ConcordiumSdk.NodeApi.Types;
using Dapper;
using Npgsql;

namespace Application.Api.GraphQL.EfCore;

public class MetricsWriter
{
    private readonly DatabaseSettings _settings;
    private readonly IMemoryCachedValue<long> _cumulativeTransactionCountCache;
    private readonly IMemoryCachedValue<long> _cumulativeAccountsCreatedCache;

    public MetricsWriter(DatabaseSettings settings, IMemoryCachedValue<long> cumulativeTransactionCountCache, IMemoryCachedValue<long> cumulativeAccountsCreatedCache)
    {
        _settings = settings;
        _cumulativeTransactionCountCache = cumulativeTransactionCountCache;
        _cumulativeAccountsCreatedCache = cumulativeAccountsCreatedCache;
    }

    public async Task AddBlockMetrics(Block block)
    {
        await using var conn = new NpgsqlConnection(_settings.ConnectionString);
        conn.Open();

        var blockParam = new
        {
            Time = block.BlockSlotTime,
            block.BlockHeight,
            BlockTimeSecs = block.BlockStatistics.BlockTime,
            TotalMicroCcd = (long)block.BalanceStatistics.TotalAmount,
            TotalEncryptedMicroCcd = (long)block.BalanceStatistics.TotalEncryptedAmount
        };

        var sql = @"insert into metrics_blocks (time, block_height, block_time_secs, total_microccd, total_encrypted_microccd) 
                    values (@Time, @BlockHeight, @BlockTimeSecs, @TotalMicroCcd, @TotalEncryptedMicroCcd)";
        await conn.ExecuteAsync(sql, blockParam);
    }

    public async Task AddTransactionMetrics(BlockInfo blockInfo, BlockSummary blockSummary)
    {
        await using var conn = new NpgsqlConnection(_settings.ConnectionString);
        conn.Open();

        var cumulativeTransactionCount = _cumulativeTransactionCountCache.GetCommittedValue();
        if (!cumulativeTransactionCount.HasValue)
        {
            var initSql = @"select max(cumulative_transaction_count)
                            from metrics_transactions
                            where time = (select max(time) from metrics_transactions)";
            var maxCumulativeTxCount = await conn.QuerySingleOrDefaultAsync<long?>(initSql);
            cumulativeTransactionCount = maxCumulativeTxCount ?? 0;
        }

        var transactionParams = blockSummary.TransactionSummaries.Select((txs, ix) => new
        {
            CumulativeTransactionCount = cumulativeTransactionCount.Value + ix + 1,
            Time = blockInfo.BlockSlotTime,
            TransactionType = TransactionTypeUnion.CreateFrom(txs.Type).ToCompactString(),
            MicroCcdCost = Convert.ToInt64(txs.Cost.MicroCcdValue),
            Success = txs.Result is TransactionSuccessResult
        }).ToArray();
        
        var sql = "insert into metrics_transactions (time, cumulative_transaction_count, transaction_type, micro_ccd_cost, success) values (@Time, @CumulativeTransactionCount, @TransactionType, @MicroCcdCost, @Success)";
        await conn.ExecuteAsync(sql, transactionParams);

        var newValue = cumulativeTransactionCount.Value + blockSummary.TransactionSummaries.Length;
        _cumulativeTransactionCountCache.EnqueueUpdate(newValue);
    }

    public async Task AddAccountsMetrics(BlockInfo blockInfo, AccountInfo[] createdAccounts)
    {
        await using var conn = new NpgsqlConnection(_settings.ConnectionString);
        conn.Open();

        var cumulativeAccountsCreated = _cumulativeAccountsCreatedCache.GetCommittedValue();
        if (!cumulativeAccountsCreated.HasValue)
        {
            var initSql = @"select max(cumulative_accounts_created)
                            from metrics_accounts
                            where time = (select max(time) from metrics_accounts)";
            var maxTotalAccounts = await conn.QuerySingleOrDefaultAsync<long?>(initSql);
            cumulativeAccountsCreated = maxTotalAccounts ?? 0;
        }

        cumulativeAccountsCreated = cumulativeAccountsCreated.Value + createdAccounts.Length;
        
        var accountsParams = new
        {
            Time = blockInfo.BlockSlotTime,
            CumulativeAccountsCreated = cumulativeAccountsCreated.Value,
            AccountsCreated = createdAccounts.Length
        };
        
        var sql = "insert into metrics_accounts (time, cumulative_accounts_created, accounts_created) values (@Time, @CumulativeAccountsCreated, @AccountsCreated)";
        await conn.ExecuteAsync(sql, accountsParams);

        _cumulativeAccountsCreatedCache.EnqueueUpdate(cumulativeAccountsCreated.Value);
    }
}
