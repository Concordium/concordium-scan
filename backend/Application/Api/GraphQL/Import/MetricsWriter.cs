using System.Data;
using System.Threading.Tasks;
using Application.Api.GraphQL.Blocks;
using Application.Api.GraphQL.EfCore.Converters.EfCore;
using Application.Api.GraphQL.Payday;
using Application.Api.GraphQL.Transactions;
using Application.Common.Diagnostics;
using Application.Database;
using ConcordiumSdk.NodeApi.Types;
using Dapper;
using Npgsql;
using PaydayPoolRewardSpecialEvent = Application.Api.GraphQL.Blocks.PaydayPoolRewardSpecialEvent;
using SpecialEvent = Application.Api.GraphQL.Blocks.SpecialEvent;

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
            TotalMicroCcdUnlocked = (long?)stats.TotalAmountUnlocked,
            TotalMicroCcdEncrypted = (long)stats.TotalAmountEncrypted,
            TotalMicroCcdStaked = (long)stats.TotalAmountStaked,
            TotalPercentageReleased = totalPercentageReleased, 
            TotalPercentageEncrypted = totalPercentageEncrypted,
            TotalPercentageStaked = totalPercentageStaked
        };

        var sql = @"insert into metrics_blocks (
                time, 
                block_height, 
                block_time_secs, 
                total_microccd, 
                total_microccd_released,
                total_microccd_unlocked,
                total_microccd_encrypted, 
                total_microccd_staked, 
                total_percentage_released, 
                total_percentage_encrypted, 
                total_percentage_staked
            ) values (
                @Time, 
                @BlockHeight, 
                @BlockTimeSecs, 
                @TotalMicroCcd, 
                @TotalMicroCcdReleased,
                @TotalMicroCcdUnlocked,
                @TotalMicroCcdEncrypted, 
                @TotalMicroCcdStaked, 
                @TotalPercentageReleased, 
                @TotalPercentageEncrypted, 
                @TotalPercentageStaked
            )";
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
            cmd.Parameters.Add(new NpgsqlParameter<long>("Amount", accountReward.TotalAmount));
            batch.BatchCommands.Add(cmd);
        }

        batch.Prepare(); // Preparing will speed up the updates, particularly when there are many!
        batch.ExecuteNonQuery();

        conn.Close();
    }

    public void AddPaydayPoolRewardMetrics(Block block, SpecialEvent[] specialEvents, RewardsSummary rewardsSummary,
        PaydaySummary? paydaySummary, PaydayPoolStakeSnapshot? paydayPoolStakeSnapshot,
        PaydayPassiveDelegationStakeSnapshot? paydayPassiveDelegationStakeSnapshot)
    {
        var poolRewards = specialEvents
            .OfType<PaydayPoolRewardSpecialEvent>()
            .ToArray();
        
        if (poolRewards.Length == 0) 
            return;
        
        // Here payday summary cannot be null. 
        // So pool rewards have length > 0 when there is payday summary
        if (paydaySummary == null) throw new ArgumentNullException(nameof(paydaySummary));
        if (paydayPoolStakeSnapshot == null) throw new ArgumentNullException(nameof(paydayPoolStakeSnapshot));
        if (paydayPassiveDelegationStakeSnapshot == null) throw new ArgumentNullException(nameof(paydayPassiveDelegationStakeSnapshot));

        using var counter = _metrics.MeasureDuration(nameof(MetricsWriter), nameof(AddPaydayPoolRewardMetrics));

        using var conn = new NpgsqlConnection(_settings.ConnectionString);
        conn.Open();

        var sql = @"
                insert into metrics_payday_pool_rewards (time, pool_id, transaction_fees_total_amount, transaction_fees_baker_amount,  
                    transaction_fees_delegator_amount, baker_reward_total_amount, baker_reward_baker_amount, baker_reward_delegator_amount,  
                    finalization_reward_total_amount, finalization_reward_baker_amount, finalization_reward_delegator_amount, sum_total_amount,              
                    sum_baker_amount, sum_delegator_amount, payday_duration_seconds, total_apy, baker_apy, delegators_apy, block_id) 
                values (@Time, @PoolId, @TransactionFeesTotalAmount, @TransactionFeesBakerAmount, 
                        @TransactionFeesDelegatorsAmount, @BakerRewardTotalAmount, @BakerRewardBakerAmount, @BakerRewardDelegatorsAmount,
                        @FinalizationRewardTotalAmount, @FinalizationRewardBakerAmount, @FinalizationRewardDelegatorsAmount, @SumTotalAmount,
                        @SumBakerAmount, @SumDelegatorsAmount, @PaydayDurationSeconds, @TotalApy, @BakerApy, @DelegatorsApy, @BlockId)";
        
        var batch = conn.CreateBatch();
        foreach (var poolReward in poolRewards)
        {
            var poolId = PoolRewardTargetToLongConverter.ConvertToLong(poolReward.Pool);
            
            var bakerRewardsSummary = poolReward.Pool switch
            {
                BakerPoolRewardTarget baker => rewardsSummary.AggregatedAccountRewards.Single(x => x.AccountId == baker.BakerId),
                PassiveDelegationPoolRewardTarget => null,
                _ => throw new NotImplementedException()
            };
            
            var stakeSnapshot = poolReward.Pool switch
            {
                BakerPoolRewardTarget baker => paydayPoolStakeSnapshot.Items.Single(x => x.BakerId == baker.BakerId),
                PassiveDelegationPoolRewardTarget => new PaydayPoolStakeSnapshotItem(-1, 0, paydayPassiveDelegationStakeSnapshot.DelegatedStake),
                _ => throw new NotImplementedException()
            };

            var cmd = batch.CreateBatchCommand();
            cmd.CommandText = sql;

            var transactionFeesTotal = (long)poolReward.TransactionFees;
            var transactionFeesBaker = bakerRewardsSummary?.TotalAmountByType.SingleOrDefault(x => x.RewardType == RewardType.TransactionFeeReward)?.TotalAmount ?? 0;
            var transactionFeesDelegators = transactionFeesTotal - transactionFeesBaker;

            var bakerRewardTotal = (long)poolReward.BakerReward;
            var bakerRewardBaker = bakerRewardsSummary?.TotalAmountByType.SingleOrDefault(x => x.RewardType == RewardType.BakerReward)?.TotalAmount ?? 0;
            var bakerRewardDelegators = bakerRewardTotal - bakerRewardBaker;

            var finalizationRewardTotal = (long)poolReward.FinalizationReward;
            var finalizationRewardBaker = bakerRewardsSummary?.TotalAmountByType.SingleOrDefault(x => x.RewardType == RewardType.FinalizationReward)?.TotalAmount ?? 0;
            var finalizationRewardDelegators = finalizationRewardTotal - finalizationRewardBaker;

            var sumTotal = transactionFeesTotal + bakerRewardTotal + finalizationRewardTotal;
            var sumBaker = transactionFeesBaker + bakerRewardBaker + finalizationRewardBaker;
            var sumDelegators = transactionFeesDelegators + bakerRewardDelegators + finalizationRewardDelegators;

            var totalApy = CalculateApy(sumTotal, stakeSnapshot.BakerStake + stakeSnapshot.DelegatedStake, paydaySummary.PaydayDurationSeconds);
            var bakerApy = CalculateApy(sumBaker, stakeSnapshot.BakerStake, paydaySummary.PaydayDurationSeconds);
            var delegatorsApy = CalculateApy(sumDelegators, stakeSnapshot.DelegatedStake, paydaySummary.PaydayDurationSeconds);
            
            cmd.Parameters.Add(new NpgsqlParameter<DateTime>("Time", block.BlockSlotTime.UtcDateTime));
            cmd.Parameters.Add(new NpgsqlParameter<long>("PoolId", poolId));
            cmd.Parameters.Add(new NpgsqlParameter<long>("TransactionFeesTotalAmount", transactionFeesTotal));
            cmd.Parameters.Add(new NpgsqlParameter<long>("TransactionFeesBakerAmount", transactionFeesBaker));
            cmd.Parameters.Add(new NpgsqlParameter<long>("TransactionFeesDelegatorsAmount", transactionFeesDelegators));
            cmd.Parameters.Add(new NpgsqlParameter<long>("BakerRewardTotalAmount", bakerRewardTotal));
            cmd.Parameters.Add(new NpgsqlParameter<long>("BakerRewardBakerAmount", bakerRewardBaker));
            cmd.Parameters.Add(new NpgsqlParameter<long>("BakerRewardDelegatorsAmount", bakerRewardDelegators));
            cmd.Parameters.Add(new NpgsqlParameter<long>("FinalizationRewardTotalAmount", finalizationRewardTotal));
            cmd.Parameters.Add(new NpgsqlParameter<long>("FinalizationRewardBakerAmount", finalizationRewardBaker));
            cmd.Parameters.Add(new NpgsqlParameter<long>("FinalizationRewardDelegatorsAmount", finalizationRewardDelegators));
            cmd.Parameters.Add(new NpgsqlParameter<long>("SumTotalAmount", sumTotal));
            cmd.Parameters.Add(new NpgsqlParameter<long>("SumBakerAmount", sumBaker));
            cmd.Parameters.Add(new NpgsqlParameter<long>("SumDelegatorsAmount", sumDelegators));
            cmd.Parameters.Add(new NpgsqlParameter<long>("PaydayDurationSeconds", paydaySummary.PaydayDurationSeconds));
            cmd.Parameters.Add(totalApy.HasValue ? new NpgsqlParameter<double>("TotalApy", totalApy.Value) : new NpgsqlParameter<double?>("TotalApy", null));
            cmd.Parameters.Add(bakerApy.HasValue ? new NpgsqlParameter<double>("BakerApy", bakerApy.Value) : new NpgsqlParameter<double?>("BakerApy", null));
            cmd.Parameters.Add(delegatorsApy.HasValue ? new NpgsqlParameter<double>("DelegatorsApy", delegatorsApy.Value) : new NpgsqlParameter<double?>("DelegatorsApy", null));
            cmd.Parameters.Add(new NpgsqlParameter<long>("BlockId", block.Id));

            batch.BatchCommands.Add(cmd);
        }

        batch.Prepare(); // Preparing will speed up the updates, particularly when there are many!
        batch.ExecuteNonQuery();

        conn.Close();
    }

    public static double? CalculateApy(long rewardAmount, long stakedAmount, long paydayDurationSeconds)
    {
        if (paydayDurationSeconds <= 0) throw new ArgumentException("Duration must be greater than zero");
        if (stakedAmount < 0) throw new ArgumentException("Duration must be greater than or equal to zero");

        if (stakedAmount == 0) return null;
        
        const double secondsPerYear = 31536000;
        var rate = rewardAmount / (double)stakedAmount;
        var compoundingPeriods = secondsPerYear / paydayDurationSeconds;
        var apy = Math.Pow(1 + rate, compoundingPeriods) - 1;

        return apy;
    }
}
