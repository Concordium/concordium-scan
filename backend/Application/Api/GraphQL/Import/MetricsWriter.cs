using System.Threading.Tasks;
using Application.Common.Diagnostics;
using Application.Database;
using ConcordiumSdk.NodeApi.Types;
using Dapper;
using Npgsql;

namespace Application.Api.GraphQL.Import;

public class MetricsWriter
{
    private readonly DatabaseSettings _settings;
    private readonly IMetrics _metrics;

    public MetricsWriter(DatabaseSettings settings, IMetrics metrics)
    {
        _settings = settings;
        _metrics = metrics;
    }

    public async Task AddBlockMetrics(Block block)
    {
        using var counter = _metrics.MeasureDuration(nameof(MetricsWriter), nameof(AddBlockMetrics));

        await using var conn = new NpgsqlConnection(_settings.ConnectionString);
        conn.Open();

        var blockParam = new
        {
            Time = block.BlockSlotTime,
            block.BlockHeight,
            BlockTimeSecs = block.BlockStatistics.BlockTime,
            TotalMicroCcd = (long)block.BalanceStatistics.TotalAmount,
            TotalMicroCcdEncrypted = (long)block.BalanceStatistics.TotalAmountEncrypted,
            TotalMicroCcdStaked = (long)block.BalanceStatistics.TotalAmountStaked
        };

        var sql = @"insert into metrics_blocks (time, block_height, block_time_secs, total_microccd, total_microccd_encrypted, total_microccd_staked) 
                    values (@Time, @BlockHeight, @BlockTimeSecs, @TotalMicroCcd, @TotalMicroCcdEncrypted, @TotalMicroCcdStaked)";
        await conn.ExecuteAsync(sql, blockParam);
    }

    public async Task AddTransactionMetrics(BlockInfo blockInfo, BlockSummary blockSummary, ImportState importState)
    {
        using var counter = _metrics.MeasureDuration(nameof(MetricsWriter), nameof(AddTransactionMetrics));

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
        using var counter = _metrics.MeasureDuration(nameof(MetricsWriter), nameof(AddAccountsMetrics));

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

        using var counter = _metrics.MeasureDuration(nameof(MetricsWriter), nameof(UpdateFinalizationTimes));

        var sql = @"update metrics_blocks  
                    set finalization_time_secs = @FinalizationTimeSecs
                    where time = @BlockSlotTime and block_height = @BlockHeight";
        await using var conn = new NpgsqlConnection(_settings.ConnectionString);
        conn.Open();
        await conn.ExecuteAsync(sql, updates);
    }

    public async Task AddBakerMetrics(DateTimeOffset blockSlotTime, BakerUpdateResults results, ImportState importState)
    {
        if (results.BakersAdded > 0 || results.BakersRemoved > 0)
        {
            using var counter = _metrics.MeasureDuration(nameof(MetricsWriter), nameof(AddBakerMetrics));

            var sql = @"
                insert into metrics_bakers (time, total_baker_count, bakers_added, bakers_removed) 
                values (@Time, @TotalBakerCount, @BakersAdded, @BakersRemoved)";

            var updateBakerCount = importState.TotalBakerCount + results.BakersAdded - results.BakersRemoved;
            
            var accountsParams = new
            {
                Time = blockSlotTime,
                TotalBakerCount = updateBakerCount,
                BakersAdded = results.BakersAdded,
                BakersRemoved = results.BakersRemoved
            };

            await using var conn = new NpgsqlConnection(_settings.ConnectionString);
            conn.Open();
            await conn.ExecuteAsync(sql, accountsParams);

            importState.TotalBakerCount = updateBakerCount;
        }
    }
}
