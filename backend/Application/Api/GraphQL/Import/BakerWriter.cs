﻿using System.Linq.Expressions;
using System.Threading.Tasks;
using Application.Api.GraphQL.Bakers;
using Application.Api.GraphQL.EfCore;
using Application.Common.Diagnostics;
using Concordium.Sdk.Types;
using Dapper;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Baker = Application.Api.GraphQL.Bakers.Baker;

namespace Application.Api.GraphQL.Import;

public class BakerWriter
{
    private readonly IDbContextFactory<GraphQlDbContext> _dbContextFactory;
    private readonly IMetrics _metrics;

    public BakerWriter(IDbContextFactory<GraphQlDbContext> dbContextFactory, IMetrics metrics)
    {
        _dbContextFactory = dbContextFactory;
        _metrics = metrics;
    }

    public async Task AddBakers(IEnumerable<Baker> bakers)
    {
        using var counter = _metrics.MeasureDuration(nameof(BakerWriter), nameof(AddBakers));

        await using var context = await _dbContextFactory.CreateDbContextAsync();
        context.Bakers.AddRange(bakers);
        await context.SaveChangesAsync();
    }

    public async Task<Baker> AddOrUpdateBaker<TSource>(TSource item, Func<TSource, ulong> bakerIdSelector, Func<TSource, Baker> createNew, Action<TSource, Baker> updateExisting)
    {
        using var counter = _metrics.MeasureDuration(nameof(BakerWriter), nameof(AddOrUpdateBaker));

        await using var context = await _dbContextFactory.CreateDbContextAsync();

        var bakerId = (long)bakerIdSelector(item);
        var baker = await context.Bakers.SingleOrDefaultAsync(x => x.Id == bakerId);
        if (baker == null)
        {
            baker = createNew(item);
            context.Add(baker);
        }
        else
            updateExisting(item, baker);
        await context.SaveChangesAsync();
        return baker;
    }
    
    public Task<Baker> UpdateBaker<TSource>(TSource item, Func<TSource, ulong> bakerIdSelector, Action<TSource, Baker> updateExisting)
    {
        return AddOrUpdateBaker(item, bakerIdSelector, _ => throw new InvalidOperationException("Baker did not exist in database"), updateExisting);
    }

    public async Task UpdateBakers(Action<Baker> updateAction, Expression<Func<Baker, bool>> whereClause)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        foreach (var baker in context.Bakers.Where(whereClause))
            updateAction(baker);
        
        await context.SaveChangesAsync();

    }

    public async Task<Baker[]> UpdateBakersFromAccountBaker(AccountBaker[] accountBakers, Action<Baker, AccountBaker> updateAction)
    {
        using var counter = _metrics.MeasureDuration(nameof(BakerWriter), nameof(UpdateBakersFromAccountBaker));

        var result = new List<Baker>();
        
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        foreach (var accountBaker in accountBakers)
        {
            var baker = await context.Bakers.SingleAsync(x => x.Id == (long)accountBaker.BakerInfo.BakerId.Id.Index);
            updateAction(baker, accountBaker);
            result.Add(baker);
        }
        await context.SaveChangesAsync();
        return result.ToArray();
    }

    public async Task UpdateBakersWithPendingChange(DateTimeOffset effectiveTimeEqualOrBefore, Action<Baker> updateAction)
    {
        using var counter = _metrics.MeasureDuration(nameof(BakerWriter), nameof(UpdateBakersWithPendingChange));
        
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        var sql = $"select * from graphql_bakers where active_pending_change->'data'->>'EffectiveTime' <= '{effectiveTimeEqualOrBefore:O}'";
        var bakers = await context.Bakers
            .FromSqlRaw(sql)
            .ToArrayAsync();

        foreach (var baker in bakers)
            updateAction(baker);
            
        await context.SaveChangesAsync();
    }

    public async Task<DateTimeOffset?> GetMinPendingChangeTime()
    {
        using var counter = _metrics.MeasureDuration(nameof(BakerWriter), nameof(GetMinPendingChangeTime));

        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var conn = context.Database.GetDbConnection();

        var result = await conn.ExecuteScalarAsync<string>("select min(active_pending_change->'data'->>'EffectiveTime') from graphql_bakers where active_pending_change is not null");

        return result != null ? DateTimeOffset.Parse(result) : null;
    }

    public async Task UpdateStakeIfBakerActiveRestakingEarnings(AccountRewardSummary[] stakeUpdates)
    {
        using var counter = _metrics.MeasureDuration(nameof(BakerWriter), nameof(UpdateStakeIfBakerActiveRestakingEarnings));

        if (stakeUpdates.Length == 0) return;
        
        var sql = @"
            update graphql_bakers 
            set active_staked_amount = active_staked_amount + @AddedStake 
            where id = @BakerId 
              and active_restake_earnings = true";

        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var conn = context.Database.GetDbConnection();

        var batch = conn.CreateBatch();
        foreach (var stakeUpdate in stakeUpdates)
        {
            var cmd = batch.CreateBatchCommand();
            cmd.CommandText = sql;
            cmd.Parameters.Add(new NpgsqlParameter<long>("BakerId", stakeUpdate.AccountId));
            cmd.Parameters.Add(new NpgsqlParameter<long>("AddedStake", stakeUpdate.TotalAmount));
            batch.BatchCommands.Add(cmd);
        }

        await conn.OpenAsync();
        await batch.PrepareAsync(); // Preparing will speed up the updates, particularly when there are many!
        await batch.ExecuteNonQueryAsync();
        await conn.CloseAsync();
    }

    public async Task<ulong> GetTotalAmountStaked()
    {
        using var counter = _metrics.MeasureDuration(nameof(BakerWriter), nameof(GetTotalAmountStaked));

        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var conn = context.Database.GetDbConnection();
        
        var result = await conn.QuerySingleAsync<long?>("select sum(active_staked_amount) from graphql_bakers");

        return result.HasValue ? (ulong)result.Value : 0;
    }

    public async Task AddBakerTransactionRelations(IEnumerable<BakerTransactionRelation> items)
    {
        using var counter = _metrics.MeasureDuration(nameof(BakerWriter), nameof(AddBakerTransactionRelations));

        await using var context = await _dbContextFactory.CreateDbContextAsync();
        context.BakerTransactionRelations.AddRange(items);
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Removes delegator information tracked for an account.
    /// Throws for accounts with no delegation information.
    /// </summary>
    public async Task RemoveDelegator(DelegatorId delegatorId) {
        using var counter = _metrics.MeasureDuration(nameof(BakerWriter), nameof(RemoveDelegator));
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var account = await context.Accounts.SingleAsync(x => x.Id == (long) delegatorId.Id.Index);
        if (account.Delegation == null) throw new InvalidOperationException("Trying to remove delegator, but account is not delegating.");
        // Update the delegation counter on the target.
        switch (account.Delegation.DelegationTarget) {
            case PassiveDelegationTarget passiveTarget:
                var passive = await context.PassiveDelegations.SingleAsync();
                passive.DelegatorCount -= 1;
                break;
            case BakerDelegationTarget target:
                var baker = await context.Bakers.SingleAsync(baker => baker.BakerId == target.BakerId);
                var activeState = baker.State as ActiveBakerState ?? throw new InvalidOperationException("Trying to remove delegator targeting a baker pool, but the baker state is not active.");
                var pool = activeState.Pool ?? throw new InvalidOperationException("Trying to remove delegator targeting a baker pool, but the baker state had no pool information.");
                pool.DelegatorCount -= 1;
                break;
        };
        // Delete the delegation information
        account.Delegation = null;
        await context.SaveChangesAsync();
    }

    public async Task UpdateDelegatedStake()
    {
        using var counter = _metrics.MeasureDuration(nameof(BakerWriter), nameof(UpdateDelegatedStake));

        var sql = @"with new_values as 
                    (
                        select 
                            baker.id as baker_id, 
                            (select coalesce(sum(acct.delegation_staked_amount), 0) 
                                from graphql_accounts acct 
                                where acct.delegation_target_baker_id = baker.id) as delegated_stake 
                        from graphql_bakers baker 
                        where baker.active_pool_open_status is not null
                    )
                    update graphql_bakers baker set
                        active_pool_delegated_stake = new_values.delegated_stake,
                        active_pool_total_stake = new_values.delegated_stake + active_staked_amount
                    from new_values 
                    where new_values.baker_id = baker.id";
        
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var conn = context.Database.GetDbConnection();
        
        await conn.ExecuteAsync(sql);
    }

    public async Task UpdateDelegatedStakeCap(ulong totalStakedAmount, decimal capitalBound, decimal leverageFactor)
    {
        using var counter = _metrics.MeasureDuration(nameof(BakerWriter), nameof(UpdateDelegatedStakeCap));

        var param = new
        {
            TotalStaked = (long)totalStakedAmount,
            CapitalBound = capitalBound,
            LeverageFactor = leverageFactor
        };

        var activePoolDelegatedStakeCap = capitalBound == 1 ? 
            @"(@LeverageFactor - 1.0) * active_staked_amount" : 
            @"least(
                floor((@CapitalBound * (@TotalStaked - active_pool_delegated_stake) - active_staked_amount) / (1 - @CapitalBound)),
                (@LeverageFactor - 1.0) * active_staked_amount)";
        
        var sql = $@"update graphql_bakers 
                        set active_pool_delegated_stake_cap = 
                            greatest(
                                0,{activePoolDelegatedStakeCap}) 
                        where active_pool_open_status is not null;";
        
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var conn = context.Database.GetDbConnection();
        
        await conn.ExecuteAsync(sql, param);
    }

    public async Task<PaydayPoolStakeSnapshot> GetPaydayPoolStakeSnapshot()
    {
        using var counter = _metrics.MeasureDuration(nameof(BakerWriter), nameof(GetPaydayPoolStakeSnapshot));

        string sql = @"select id as BakerId, active_pool_payday_status_baker_stake as BakerStake, active_pool_payday_status_delegated_stake as DelegatedStake 
                       from graphql_bakers 
                       where active_pool_open_status is not null";

        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var conn = context.Database.GetDbConnection();
        
        var items = await conn.QueryAsync<PaydayPoolStakeSnapshotItem>(sql);

        return new PaydayPoolStakeSnapshot(items.ToArray());
    }

    public async Task CreateTemporaryBakerPoolPaydayStatuses()
    {
        using var counter = _metrics.MeasureDuration(nameof(BakerWriter), nameof(CreateTemporaryBakerPoolPaydayStatuses));

        var sql = @"
            insert into graphql_pool_payday_stakes (payout_block_id, pool_id, baker_stake, delegated_stake)
            select -1,
                   id,
                   active_pool_payday_status_baker_stake,
                   active_pool_payday_status_delegated_stake
            from graphql_bakers
            where active_pool_payday_status_baker_stake is not null;";
        
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var conn = context.Database.GetDbConnection();
        
        await conn.ExecuteAsync(sql);
    }
    
    public async Task UpdateTemporaryBakerPoolPaydayStatusesWithPayoutBlockId(long payoutBlockId)
    {
        using var counter = _metrics.MeasureDuration(nameof(BakerWriter), nameof(CreateTemporaryBakerPoolPaydayStatuses));

        var param = new
        {
            PayoutBlockId = payoutBlockId
        };
        
        var sql = @"
            update graphql_pool_payday_stakes set payout_block_id = @PayoutBlockId where payout_block_id = -1;";
        
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var conn = context.Database.GetDbConnection();
        
        await conn.ExecuteAsync(sql, param);
    }
}
