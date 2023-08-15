using System.Threading.Tasks;
using Application.Api.GraphQL.Blocks;
using Application.Api.GraphQL.EfCore;
using Application.Common.Diagnostics;
using Concordium.Sdk.Types;
using Dapper;
using Microsoft.EntityFrameworkCore;

namespace Application.Api.GraphQL.Import;

public class BlockWriter
{
    private readonly IDbContextFactory<GraphQlDbContext> _dbContextFactory;
    private readonly IMetrics _metrics;
    private readonly BlockChangeCalculator _changeCalculator;

    public BlockWriter(IDbContextFactory<GraphQlDbContext> dbContextFactory, IMetrics metrics)
    {
        _dbContextFactory = dbContextFactory;
        _changeCalculator = new BlockChangeCalculator(new InitialTokenReleaseScheduleRepository());
        _metrics = metrics;
    }

    public async Task<Block> AddBlock(
        BlockInfo blockInfo,
        RewardOverviewBase rewardStatus,
        int chainParametersId,
        BakerUpdateResults bakerUpdateResults,
        DelegationUpdateResults delegationUpdateResults,
        ImportState importState,
        ulong[] nonCirculatingAccountIds)
    {
        using var counter = _metrics.MeasureDuration(nameof(BlockWriter), nameof(AddBlock));

        await using var context = await _dbContextFactory.CreateDbContextAsync();

        var blockTime = GetBlockTime(blockInfo, importState.LastBlockSlotTime);
        importState.LastBlockSlotTime = blockInfo.BlockSlotTime;

        var nonCirculatingAccountsBalance = await GetNonCirculatingBalance(nonCirculatingAccountIds);

        var calculateTotalAmountUnlocked = _changeCalculator.CalculateTotalAmountUnlocked(blockInfo.BlockSlotTime, importState.GenesisBlockHash);
        var balanceStatistics = BalanceStatistics.MapBalanceStatistics(
            rewardStatus, 
            blockInfo.BlockSlotTime, 
            bakerUpdateResults, 
            delegationUpdateResults, 
            importState, 
            nonCirculatingAccountsBalance, calculateTotalAmountUnlocked);

        var block = Block.MapBlock(
            blockInfo,
            blockTime,
            chainParametersId,
            balanceStatistics);
        context.Blocks.Add(block);

        await context.SaveChangesAsync(); // assign ID to block!

        await context.SaveChangesAsync();
        return block;
    }

    private async Task<ulong> GetNonCirculatingBalance(ulong[] nonCirculatingAccountIds)
    {

        ulong nonCirculatingAccountsBalance = 0;
        if (nonCirculatingAccountIds.Length > 0)
        {
            using var context = _dbContextFactory.CreateDbContext();
            using var cmd = (Npgsql.NpgsqlCommand)context.Database.GetDbConnection().CreateCommand();
            await cmd.Connection.OpenAsync();
            cmd.CommandText = @"SELECT COALESCE(SUM(ccd_amount)::bigint, 0::bigint) FROM graphql_accounts WHERE id = ANY(@AccountIds::bigint[])";
            cmd.Parameters.AddWithValue("AccountIds", NpgsqlTypes.NpgsqlDbType.Array | NpgsqlTypes.NpgsqlDbType.Bigint, nonCirculatingAccountIds);
            nonCirculatingAccountsBalance = (ulong)(long)cmd.ExecuteScalar();
            await cmd.Connection.CloseAsync();
        }

        return nonCirculatingAccountsBalance;
    }

    public async Task<Blocks.SpecialEvent[]> AddSpecialEvents(Block block, IList<ISpecialEvent> specialEvents)
    {
        using var counter = _metrics.MeasureDuration(nameof(BlockWriter), nameof(AddSpecialEvents));
        
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var specialEventsMapped = SpecialEvent.MapSpecialEvents(block.Id, specialEvents).ToArray();
        context.SpecialEvents.AddRange(specialEventsMapped);
        
        await context.SaveChangesAsync();
        return specialEventsMapped;
    }

    private double GetBlockTime(BlockInfo blockInfo, DateTimeOffset previousBlockSlotTime)
    {
        var blockTime = blockInfo.BlockSlotTime - previousBlockSlotTime;
        return Math.Round(blockTime.TotalSeconds, 1);
    }

    public async Task UpdateTotalAmountLockedInReleaseSchedules(Block block)
    {
        using var counter = _metrics.MeasureDuration(nameof(BlockWriter), nameof(UpdateTotalAmountLockedInReleaseSchedules));

        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var conn = context.Database.GetDbConnection();
        
        var sql = "select sum(amount) from graphql_account_release_schedule where timestamp > @BlockSlotTime";
        var result = await conn.ExecuteScalarAsync<long>(sql, new { block.BlockSlotTime });

        context.Blocks.Attach(block);
        block.BalanceStatistics.TotalAmountLockedInReleaseSchedules = (ulong)result;
        await context.SaveChangesAsync();
    }
    
    internal async Task<FinalizationTimeUpdate> UpdateFinalizationTimeOnBlocksInFinalizationProof(BlockInfo block, ImportState importState)
    {
        using var counter = _metrics.MeasureDuration(nameof(BlockWriter), nameof(UpdateFinalizationTimeOnBlocksInFinalizationProof));

        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var finalizedBlockHeights = await context.Blocks
            .Where(x => x.BlockHash == block.BlockLastFinalized.ToString())
            .Select(x => x.BlockHeight)
            .ToArrayAsync();

        if (finalizedBlockHeights.Length != 1) throw new InvalidOperationException();
        var maxBlockHeight = finalizedBlockHeights.Single();
        var finalizationTimeUpdate = new FinalizationTimeUpdate(importState.MaxBlockHeightWithUpdatedFinalizationTime, maxBlockHeight);
        if (finalizationTimeUpdate.MinBlockHeight == finalizationTimeUpdate.MaxBlockHeight)
        {
            return finalizationTimeUpdate;
        }

        const string sql = @"
update graphql_blocks 
set 
    block_stats_finalization_time_secs = sub_query.finalizationSeconds
from (
    select 
        block_height,
        ROUND(EXTRACT(EPOCH FROM (@BlockSlotTime - block_slot_time)),1) as finalizationSeconds
    from graphql_blocks
    where block_height > @MinBlockHeight
        and block_height <= @MaxBlockHeight
    ) as sub_query
where graphql_blocks.block_height = sub_query.block_height";      
        
        var conn = context.Database.GetDbConnection();
        await conn.ExecuteAsync(
            sql,
        new
            {
                finalizationTimeUpdate.MinBlockHeight,
                finalizationTimeUpdate.MaxBlockHeight,
                block.BlockSlotTime
            });
        
        importState.MaxBlockHeightWithUpdatedFinalizationTime = maxBlockHeight;
        
        return finalizationTimeUpdate;
    }
}
