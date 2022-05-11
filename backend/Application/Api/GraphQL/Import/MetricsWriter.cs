using System.Threading.Tasks;
using Application.Api.GraphQL.Blocks;
using Application.Api.GraphQL.Transactions;
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

        var stats = block.BalanceStatistics;

        var totalPercentageReleased = stats.TotalAmountReleased.HasValue ? CalculatePercentage(stats.TotalAmountReleased.Value, stats.TotalAmount) : (double?)null;
        var totalPercentageEncrypted = CalculatePercentage(stats.TotalAmountEncrypted, stats.TotalAmount);
        var totalPercentageStaked = CalculatePercentage(stats.TotalAmountStaked, stats.TotalAmount);

        var blockParam = new
        {
            Time = block.BlockSlotTime,
            block.BlockHeight,
            BlockTimeSecs = block.BlockStatistics.BlockTime,
            TotalMicroCcd = (long)stats.TotalAmount,
            TotalMicroCcdReleased = (long?)stats.TotalAmountReleased,
            TotalMicroCcdEncrypted = (long)stats.TotalAmountEncrypted,
            TotalMicroCcdStaked = (long)stats.TotalAmountStaked,
            TotalPercentageReleased = totalPercentageReleased, 
            TotalPercentageEncrypted = totalPercentageEncrypted,
            TotalPercentageStaked = totalPercentageStaked
        };

        var sql = @"insert into metrics_blocks (time, block_height, block_time_secs, total_microccd, total_microccd_released, total_microccd_encrypted, total_microccd_staked, total_percentage_released, total_percentage_encrypted, total_percentage_staked) 
                    values (@Time, @BlockHeight, @BlockTimeSecs, @TotalMicroCcd, @TotalMicroCcdReleased, @TotalMicroCcdEncrypted, @TotalMicroCcdStaked, @TotalPercentageReleased, @TotalPercentageEncrypted, @TotalPercentageStaked)";
        await conn.ExecuteAsync(sql, blockParam);
    }

    private static double CalculatePercentage(ulong numerator, ulong denominator)
    {
        return Math.Round(numerator * 1.0 / denominator, 10);
    }

    public async Task AddTransactionMetrics(BlockInfo blockInfo, BlockSummaryBase blockSummary, ImportState importState)
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
        if (results.BakersAddedCount > 0 || results.BakersRemovedCount > 0)
        {
            using var counter = _metrics.MeasureDuration(nameof(MetricsWriter), nameof(AddBakerMetrics));

            var sql = @"
                insert into metrics_bakers (time, total_baker_count, bakers_added, bakers_removed) 
                values (@Time, @TotalBakerCount, @BakersAdded, @BakersRemoved)";

            var updateBakerCount = importState.TotalBakerCount + results.BakersAddedCount - results.BakersRemovedCount;
            
            var accountsParams = new
            {
                Time = blockSlotTime,
                TotalBakerCount = updateBakerCount,
                BakersAdded = results.BakersAddedCount,
                BakersRemoved = results.BakersRemovedCount
            };

            await using var conn = new NpgsqlConnection(_settings.ConnectionString);
            conn.Open();
            await conn.ExecuteAsync(sql, accountsParams);
            await conn.CloseAsync();
            importState.TotalBakerCount = updateBakerCount;
        }
    }

    public void AddRewardMetrics(DateTimeOffset blockSlotTime, RewardsSummary rewards)
    {
        using var counter = _metrics.MeasureDuration(nameof(MetricsWriter), nameof(AddRewardMetrics));
        
        var sql = @"
                insert into metrics_rewards (time, account_id, amount) 
                values (@Time, @AccountId, @Amount)";

        using var conn = new NpgsqlConnection(_settings.ConnectionString);
        conn.Open();
        
        var batch = conn.CreateBatch();
        foreach (var accountReward in rewards.AggregatedAccountRewards)
        {
            var cmd = batch.CreateBatchCommand();
            cmd.CommandText = sql;
            cmd.Parameters.Add(new NpgsqlParameter<DateTime>("Time", blockSlotTime.UtcDateTime));
            cmd.Parameters.Add(new NpgsqlParameter<long>("AccountId", accountReward.AccountId));
            cmd.Parameters.Add(new NpgsqlParameter<long>("Amount", accountReward.RewardAmount));
            batch.BatchCommands.Add(cmd);
        }

        batch.Prepare(); // Preparing will speed up the updates, particularly when there are many!
        batch.ExecuteNonQuery();

        conn.Close();
    }
}
