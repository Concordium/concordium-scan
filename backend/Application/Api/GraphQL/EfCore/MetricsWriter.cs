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

    public MetricsWriter(DatabaseSettings settings)
    {
        _settings = settings;
    }

    public async Task AddBlockMetrics(BlockInfo blockInfo, RewardStatus rewardStatus,
        IMemoryCachedValue<DateTimeOffset> previousBlockSlotTimeCache)
    {
        await using var conn = new NpgsqlConnection(_settings.ConnectionString);
        conn.Open();

        var previousBlockSlotTime = await GetPreviousBlockSlotTime(blockInfo, previousBlockSlotTimeCache, conn);
        var blockTime = previousBlockSlotTime.HasValue 
            ? GetBlockTime(blockInfo.BlockSlotTime, previousBlockSlotTime.Value)
            : 0;
        
        var blockParam = new
        {
            Time = blockInfo.BlockSlotTime,
            blockInfo.BlockHeight,
            BlockTimeSecs = blockTime,
            TotalMicroCcd = (long)rewardStatus.TotalAmount.MicroCcdValue,
            TotalEncryptedMicroCcd = (long)rewardStatus.TotalEncryptedAmount.MicroCcdValue
        };

        var sql = @"insert into metrics_blocks (time, block_height, block_time_secs, total_microccd, total_encrypted_microccd) 
                    values (@Time, @BlockHeight, @BlockTimeSecs, @TotalMicroCcd, @TotalEncryptedMicroCcd)";
        await conn.ExecuteAsync(sql, blockParam);
        
        previousBlockSlotTimeCache.EnqueueUpdate(blockInfo.BlockSlotTime);
    }

    public async Task AddTransactionMetrics(BlockInfo blockInfo, BlockSummary blockSummary, IMemoryCachedValue<long> cumulativeTransactionCountCache)
    {
        await using var conn = new NpgsqlConnection(_settings.ConnectionString);
        conn.Open();

        var cumulativeTransactionCount = cumulativeTransactionCountCache.GetCommittedValue();
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
        cumulativeTransactionCountCache.EnqueueUpdate(newValue);
    }

    public async Task AddAccountsMetrics(BlockInfo blockInfo, AccountInfo[] createdAccounts, IMemoryCachedValue<long> cumulativeAccountsCreatedCache)
    {
        await using var conn = new NpgsqlConnection(_settings.ConnectionString);
        conn.Open();

        var cumulativeAccountsCreated = cumulativeAccountsCreatedCache.GetCommittedValue();
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

        cumulativeAccountsCreatedCache.EnqueueUpdate(cumulativeAccountsCreated.Value);
    }

    private double GetBlockTime(DateTimeOffset currentBlockSlotTime, DateTimeOffset previousBlockSlotTime)
    {
        var blockTime = currentBlockSlotTime - previousBlockSlotTime;
        return Math.Round(blockTime.TotalSeconds, 1);
    }

    private async Task<DateTimeOffset?> GetPreviousBlockSlotTime(BlockInfo blockInfo, IMemoryCachedValue<DateTimeOffset> previousBlockSlotTimeCache, NpgsqlConnection conn)
    {
        var previousBlockSlotTime = previousBlockSlotTimeCache.GetCommittedValue();
        if (previousBlockSlotTime.HasValue) 
            return previousBlockSlotTime;
        
        if (blockInfo.BlockHeight == 0)
            return null;

        var sql = @"select time, block_height
                    from metrics_blocks  
                    order by time desc limit 1;";
            
        var expectedBlockHeight = blockInfo.BlockHeight - 1;

        var result = await conn.QuerySingleOrDefaultAsync(sql);
        if (result == null)
            throw new InvalidOperationException($"Could not find any existing block metric data!");
        if (result.block_height != expectedBlockHeight)
            throw new InvalidOperationException($"Latest block metric data did not have expected block height {expectedBlockHeight}.");
        return (DateTimeOffset)DateTime.SpecifyKind(result.time, DateTimeKind.Utc);
    }
}
