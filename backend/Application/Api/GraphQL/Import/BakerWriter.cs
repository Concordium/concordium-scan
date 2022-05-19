using System.Linq.Expressions;
using System.Threading.Tasks;
using Application.Api.GraphQL.Bakers;
using Application.Api.GraphQL.EfCore;
using Application.Common.Diagnostics;
using ConcordiumSdk.NodeApi.Types;
using Dapper;
using Microsoft.EntityFrameworkCore;
using Npgsql;

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
            var baker = await context.Bakers.SingleAsync(x => x.Id == (long)accountBaker.BakerId);
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
        await conn.OpenAsync();
        var result = await conn.ExecuteScalarAsync<string>("select min(active_pending_change->'data'->>'EffectiveTime') from graphql_bakers where active_pending_change is not null");
        await conn.CloseAsync();

        return result != null ? DateTimeOffset.Parse(result) : null;
    }

    public async Task UpdateStakeIfBakerActiveRestakingEarnings(AccountReward[] stakeUpdates)
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

        await conn.OpenAsync();

        var batch = conn.CreateBatch();
        foreach (var stakeUpdate in stakeUpdates)
        {
            var cmd = batch.CreateBatchCommand();
            cmd.CommandText = sql;
            cmd.Parameters.Add(new NpgsqlParameter<long>("BakerId", stakeUpdate.AccountId));
            cmd.Parameters.Add(new NpgsqlParameter<long>("AddedStake", stakeUpdate.RewardAmount));
            batch.BatchCommands.Add(cmd);
        }

        await batch.PrepareAsync(); // Preparing will speed up the updates, particularly when there are many!
        await batch.ExecuteNonQueryAsync();
        
        await conn.CloseAsync();
    }

    public async Task<ulong> GetTotalAmountStaked()
    {
        using var counter = _metrics.MeasureDuration(nameof(BakerWriter), nameof(GetTotalAmountStaked));

        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var conn = context.Database.GetDbConnection();

        await conn.OpenAsync();
        var result = await conn.QuerySingleAsync<long?>("select sum(active_staked_amount) from graphql_bakers");
        await conn.CloseAsync();

        return result.HasValue ? (ulong)result.Value : 0;
    }

    public async Task AddBakerTransactionRelations(IEnumerable<BakerTransactionRelation> items)
    {
        using var counter = _metrics.MeasureDuration(nameof(BakerWriter), nameof(AddBakerTransactionRelations));

        await using var context = await _dbContextFactory.CreateDbContextAsync();
        context.BakerTransactionRelations.AddRange(items);
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

        await conn.OpenAsync();
        await conn.ExecuteAsync(sql);
        await conn.CloseAsync();
    }
}
