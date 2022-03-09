using System.Threading.Tasks;
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

    public async Task AddTransactionMetrics(BlockInfo blockInfo, BlockSummary blockSummary, ImportState importState)
    {
        await using var conn = new NpgsqlConnection(_settings.ConnectionString);
        conn.Open();

        var cumulativeTransactionCount = importState.CumulativeTransactionCount;

        var transactionParams = blockSummary.TransactionSummaries.Select((txs, ix) => new
        {
            CumulativeTransactionCount = cumulativeTransactionCount + ix + 1,
            Time = blockInfo.BlockSlotTime,
            TransactionType = TransactionTypeUnion.CreateFrom(txs.Type).ToCompactString(),
            MicroCcdCost = Convert.ToInt64(txs.Cost.MicroCcdValue),
            Success = txs.Result is TransactionSuccessResult
        }).ToArray();
        
        var sql = "insert into metrics_transactions (time, cumulative_transaction_count, transaction_type, micro_ccd_cost, success) values (@Time, @CumulativeTransactionCount, @TransactionType, @MicroCcdCost, @Success)";
        await conn.ExecuteAsync(sql, transactionParams);

        var newValue = cumulativeTransactionCount + blockSummary.TransactionSummaries.Length;
        importState.CumulativeTransactionCount = newValue;
    }

    public async Task AddAccountsMetrics(BlockInfo blockInfo, AccountInfo[] createdAccounts, ImportState importState)
    {
        await using var conn = new NpgsqlConnection(_settings.ConnectionString);
        conn.Open();

        var cumulativeAccountsCreated = importState.CumulativeAccountsCreated + createdAccounts.Length;
        
        var accountsParams = new
        {
            Time = blockInfo.BlockSlotTime,
            CumulativeAccountsCreated = cumulativeAccountsCreated,
            AccountsCreated = createdAccounts.Length
        };
        
        var sql = "insert into metrics_accounts (time, cumulative_accounts_created, accounts_created) values (@Time, @CumulativeAccountsCreated, @AccountsCreated)";
        await conn.ExecuteAsync(sql, accountsParams);

        importState.CumulativeAccountsCreated = cumulativeAccountsCreated;
    }

    public async Task UpdateFinalizationTimes(FinalizationTimeUpdate[] updates)
    {
        if (updates.Length == 0) return;
        
        var sql = @"update metrics_blocks  
                    set finalization_time_secs = @FinalizationTimeSecs
                    where time = @BlockSlotTime and block_height = @BlockHeight";
        await using var conn = new NpgsqlConnection(_settings.ConnectionString);
        conn.Open();
        await conn.ExecuteAsync(sql, updates);
    }
}
