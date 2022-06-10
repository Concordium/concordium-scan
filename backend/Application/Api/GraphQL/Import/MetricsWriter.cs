using System.Threading.Tasks;
using Application.Api.GraphQL.Blocks;
using Application.Api.GraphQL.EfCore.Converters.EfCore;
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
            cmd.Parameters.Add(new NpgsqlParameter<long>("Amount", accountReward.TotalAmount));
            batch.BatchCommands.Add(cmd);
        }

        batch.Prepare(); // Preparing will speed up the updates, particularly when there are many!
        batch.ExecuteNonQuery();

        conn.Close();
    }

    public void AddPoolRewardMetrics(Block block, SpecialEvent[] specialEvents, RewardsSummary rewardsSummary)
    {
        var poolRewards = specialEvents
            .OfType<PaydayPoolRewardSpecialEvent>()
            .ToArray();
        
        if (poolRewards.Length == 0) 
            return;
        
        using var counter = _metrics.MeasureDuration(nameof(MetricsWriter), nameof(AddPoolRewardMetrics));

        using var conn = new NpgsqlConnection(_settings.ConnectionString);
        conn.Open();

        var bakerPoolStakesSql = @"select id as BakerId, active_pool_total_stake as TotalStakedAmount, active_staked_amount as BakerStakedAmount, active_pool_delegated_stake as DelegatedStakedAmount from graphql_bakers where active_pool_open_status is not null";
        var bakerPoolStakes = conn.Query<PoolStakeInfo>(bakerPoolStakesSql).ToDictionary(x => x.BakerId);
        var passiveDelegationStake = conn.QuerySingle<long>("select delegated_stake from graphql_passive_delegation");
        var passiveDelegationStakes = new PoolStakeInfo(-1, passiveDelegationStake, 0, passiveDelegationStake);
            
        var sql = @"
                insert into metrics_pool_rewards (time, pool_id, total_amount, baker_amount, delegator_amount, total_staked_amount, baker_staked_amount, delegated_staked_amount, reward_type, block_id) 
                values (@Time, @PoolId, @TotalAmount, @BakerAmount, @DelegatorAmount, @TotalStakedAmount, @BakerStakedAmount, @DelegatorStakedAmount, @RewardType, @BlockId)";
        
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

            PoolStakeInfo poolStakes = poolReward.Pool switch
            {
                BakerPoolRewardTarget baker => bakerPoolStakes.ContainsKey(baker.BakerId) ? bakerPoolStakes[baker.BakerId] : new PoolStakeInfo(baker.BakerId, 0, 0, 0),
                PassiveDelegationPoolRewardTarget => passiveDelegationStakes,
                _ => throw new NotImplementedException()
            };

            AddCommand(batch, sql, block, poolId, RewardType.BakerReward, (long)poolReward.BakerReward, bakerRewardsSummary, poolStakes);
            AddCommand(batch, sql, block, poolId, RewardType.FinalizationReward, (long)poolReward.FinalizationReward, bakerRewardsSummary, poolStakes);
            AddCommand(batch, sql, block, poolId, RewardType.TransactionFeeReward, (long)poolReward.TransactionFees, bakerRewardsSummary, poolStakes);
        }

        batch.Prepare(); // Preparing will speed up the updates, particularly when there are many!
        batch.ExecuteNonQuery();

        conn.Close();
    }

    private record PoolStakeInfo(long BakerId, long TotalStakedAmount, long BakerStakedAmount, long DelegatedStakedAmount);

    private void AddCommand(NpgsqlBatch batch, string sql, Block block, long poolId, RewardType rewardType,
        long totalAmount, AccountRewardSummary? rewardSummary, PoolStakeInfo poolStakeInfo)
    {
        var cmd = batch.CreateBatchCommand();
        cmd.CommandText = sql;

        var bakerAmount = rewardSummary?.TotalAmountByType.SingleOrDefault(x => x.RewardType == rewardType)?.TotalAmount ?? 0;

        cmd.Parameters.Add(new NpgsqlParameter<DateTime>("Time", block.BlockSlotTime.UtcDateTime));
        cmd.Parameters.Add(new NpgsqlParameter<long>("PoolId", poolId));
        cmd.Parameters.Add(new NpgsqlParameter<long>("TotalAmount", totalAmount));
        cmd.Parameters.Add(new NpgsqlParameter<long>("BakerAmount", bakerAmount));
        cmd.Parameters.Add(new NpgsqlParameter<long>("DelegatorAmount", totalAmount - bakerAmount));
        cmd.Parameters.Add(new NpgsqlParameter<long>("TotalStakedAmount", poolStakeInfo.TotalStakedAmount));
        cmd.Parameters.Add(new NpgsqlParameter<long>("BakerStakedAmount", poolStakeInfo.BakerStakedAmount));
        cmd.Parameters.Add(new NpgsqlParameter<long>("DelegatorStakedAmount", poolStakeInfo.DelegatedStakedAmount));
        cmd.Parameters.Add(new NpgsqlParameter<int>("RewardType", (int)rewardType));
        cmd.Parameters.Add(new NpgsqlParameter<long>("BlockId", block.Id));

        batch.BatchCommands.Add(cmd);
    }

    public void AddPaydayPoolRewardMetrics(Block block, SpecialEvent[] specialEvents, RewardsSummary rewardsSummary)
    {
        var poolRewards = specialEvents
            .OfType<PaydayPoolRewardSpecialEvent>()
            .ToArray();
        
        if (poolRewards.Length == 0) 
            return;
        
        using var counter = _metrics.MeasureDuration(nameof(MetricsWriter), nameof(AddPoolRewardMetrics));

        using var conn = new NpgsqlConnection(_settings.ConnectionString);
        conn.Open();

        var sql = @"
                insert into metrics_payday_pool_rewards (time, pool_id, transaction_fees_total_amount, transaction_fees_baker_amount,  
                    transaction_fees_delegator_amount, baker_reward_total_amount, baker_reward_baker_amount, baker_reward_delegator_amount,  
                    finalization_reward_total_amount, finalization_reward_baker_amount, finalization_reward_delegator_amount, sum_total_amount,              
                    sum_baker_amount, sum_delegator_amount, block_id) 
                values (@Time, @PoolId, @TransactionFeesTotalAmount, @TransactionFeesBakerAmount, 
                        @TransactionFeesDelegatorsAmount, @BakerRewardTotalAmount, @BakerRewardBakerAmount, @BakerRewardDelegatorsAmount,
                        @FinalizationRewardTotalAmount, @FinalizationRewardBakerAmount, @FinalizationRewardDelegatorsAmount, @SumTotalAmount,
                        @SumBakerAmount, @SumDelegatorsAmount, @BlockId)";
        
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
            cmd.Parameters.Add(new NpgsqlParameter<long>("BlockId", block.Id));

            batch.BatchCommands.Add(cmd);
        }

        batch.Prepare(); // Preparing will speed up the updates, particularly when there are many!
        batch.ExecuteNonQuery();

        conn.Close();
    }
}
