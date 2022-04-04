using System.Threading.Tasks;
using Application.Api.GraphQL.Bakers;
using Application.Api.GraphQL.EfCore;
using Application.Common.Diagnostics;
using ConcordiumSdk.NodeApi.Types;
using Dapper;
using Microsoft.EntityFrameworkCore;

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

    public async Task AddOrUpdateBakers<TState>(IEnumerable<BakerAddOrUpdateData<TState>> items, Func<TState, Baker> insertAction, Action<TState, Baker> updateAction)
    {
        using var counter = _metrics.MeasureDuration(nameof(BakerWriter), nameof(AddOrUpdateBakers));

        await using var context = await _dbContextFactory.CreateDbContextAsync();
        foreach (var item in items)
        {
            var baker = await context.Bakers.SingleOrDefaultAsync(x => x.Id == (long)item.BakerId);
            if (baker == null)
            {
                baker = insertAction(item.State);
                context.Add(baker);
            }
            else
                updateAction(item.State, baker);
        }
        await context.SaveChangesAsync();
    }

    public async Task AddBakers(IEnumerable<Baker> bakers)
    {
        using var counter = _metrics.MeasureDuration(nameof(BakerWriter), nameof(AddBakers));

        await using var context = await _dbContextFactory.CreateDbContextAsync();
        context.Bakers.AddRange(bakers);
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

        var sql = $"select * from graphql_bakers where pending_change->'data'->>'EffectiveTime' <= '{effectiveTimeEqualOrBefore:O}'";
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
        var result = await conn.ExecuteScalarAsync<string>("select min(pending_change->'data'->>'EffectiveTime') from graphql_bakers where pending_change is not null");
        await conn.CloseAsync();

        return result != null ? DateTimeOffset.Parse(result) : null;
    }
}

public record BakerAddOrUpdateData<T>(ulong BakerId, T State);