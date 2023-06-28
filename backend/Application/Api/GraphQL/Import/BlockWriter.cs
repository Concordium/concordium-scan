using System.Threading.Tasks;
using Application.Api.GraphQL.Blocks;
using Application.Api.GraphQL.EfCore;
using Application.Common.Diagnostics;
using Concordium.Sdk.Types;
using Dapper;
using Microsoft.EntityFrameworkCore;
using FinalizationSummary = Application.Api.GraphQL.Blocks.FinalizationSummary;
using FinalizationSummaryParty = Application.Api.GraphQL.Blocks.FinalizationSummaryParty;

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
        Concordium.Sdk.Types.FinalizationSummary? finalizationSummary,
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
        FinalizationSummary.TryMapFinalizationSummary(finalizationSummary, out var possibleFinalizationSummary);

        var block = Block.MapBlock(
            blockInfo,
            blockTime,
            chainParametersId,
            balanceStatistics,
            possibleFinalizationSummary);
        context.Blocks.Add(block);

        await context.SaveChangesAsync(); // assign ID to block!
        
        if (finalizationSummary != null)
        {
            var toSave = finalizationSummary.Finalizers
                .Select((x, ix) => MapFinalizer(block, ix, x))
                .ToArray();
            context.FinalizationSummaryFinalizers.AddRange(toSave);
        }

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

    private static BlockRelated<FinalizationSummaryParty> MapFinalizer(Block block, int index, Concordium.Sdk.Types.FinalizationSummaryParty value)
    {
        return new BlockRelated<FinalizationSummaryParty>
        {
            BlockId = block.Id,
            Index = index,
            Entity = new FinalizationSummaryParty
            {
                BakerId = (long)value.BakerId.Id.Index,
                Weight = (long)value.Weight,
                Signed = value.SignaturePresent
            }
        };
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
    
    public async Task<FinalizationTimeUpdate[]> UpdateFinalizationTimeOnBlocksInFinalizationProof(Block block, ImportState importState)
    {
        if (block.FinalizationSummary == null) return Array.Empty<FinalizationTimeUpdate>();

        using var counter = _metrics.MeasureDuration(nameof(BlockWriter), nameof(UpdateFinalizationTimeOnBlocksInFinalizationProof));

        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var finalizedBlockHeights = await context.Blocks
            .Where(x => x.BlockHash == block.FinalizationSummary.FinalizedBlockHash)
            .Select(x => x.BlockHeight)
            .ToArrayAsync();

        if (finalizedBlockHeights.Length != 1) throw new InvalidOperationException();
        var maxBlockHeight = finalizedBlockHeights.Single();
        var minBlockHeight = importState.MaxBlockHeightWithUpdatedFinalizationTime;
        
        var result = await context.Blocks
            .Where(x => x.BlockHeight > minBlockHeight && x.BlockHeight <= maxBlockHeight)
            .Select(x => new FinalizationTimeUpdate(x.BlockHeight, x.BlockSlotTime,
                Math.Round((block.BlockSlotTime - x.BlockSlotTime).TotalSeconds, 1)
            ))
            .ToArrayAsync();

        var conn = context.Database.GetDbConnection();
        await conn.ExecuteAsync(
            "update graphql_blocks set block_stats_finalization_time_secs = @FinalizationTimeSecs where block_height = @BlockHeight",
            result);
        
        importState.MaxBlockHeightWithUpdatedFinalizationTime = maxBlockHeight;
        
        return result;
    }
}
