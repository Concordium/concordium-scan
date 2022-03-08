using System.Threading.Tasks;
using Application.Common;
using ConcordiumSdk.NodeApi.Types;
using Dapper;
using Microsoft.EntityFrameworkCore;

namespace Application.Api.GraphQL.EfCore;

public class BlockWriter
{
    private readonly IDbContextFactory<GraphQlDbContext> _dbContextFactory;
    private readonly IMemoryCachedValue<DateTimeOffset> _previousBlockSlotTimeCache;
    private readonly IMemoryCachedValue<long> _maxBlockHeightWithUpdatedFinalizationTimeCache;

    public BlockWriter(IDbContextFactory<GraphQlDbContext> dbContextFactory, IMemoryCachedValue<DateTimeOffset> previousBlockSlotTimeCache, IMemoryCachedValue<long> maxBlockHeightWithUpdatedFinalizationTimeCache)
    {
        _dbContextFactory = dbContextFactory;
        _previousBlockSlotTimeCache = previousBlockSlotTimeCache;
        _maxBlockHeightWithUpdatedFinalizationTimeCache = maxBlockHeightWithUpdatedFinalizationTimeCache;
    }

    public async Task<Block> AddBlock(BlockInfo blockInfo, BlockSummary blockSummary, RewardStatus rewardStatus)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        await _previousBlockSlotTimeCache.EnsureInitialized(() => InitializePreviousBlockSlotTime(blockInfo, context));
        _previousBlockSlotTimeCache.EnqueueUpdate(blockInfo.BlockSlotTime);
        
        var block = MapBlock(blockInfo, blockSummary, rewardStatus);
        context.Blocks.Add(block);
        
        await context.SaveChangesAsync(); // assign ID to block!

        var finalizationRewards = blockSummary.SpecialEvents.OfType<FinalizationRewardsSpecialEvent>().SingleOrDefault();
        if (finalizationRewards != null)
        {
            var toSave = finalizationRewards.FinalizationRewards
                .Select((x, ix) => MapFinalizationReward(block, ix, x))
                .ToArray();
            context.FinalizationRewards.AddRange(toSave);
        }

        var bakingRewards = blockSummary.SpecialEvents.OfType<BakingRewardsSpecialEvent>().SingleOrDefault();
        if (bakingRewards != null)
        {
            var toSave = bakingRewards.BakerRewards
                .Select((x, ix) => MapBakingReward(block, ix, x))
                .ToArray();
            context.BakingRewards.AddRange(toSave);
        }

        var finalizationData = blockSummary.FinalizationData;
        if (finalizationData != null)
        {
            var toSave = finalizationData.Finalizers
                .Select((x, ix) => MapFinalizer(block, ix, x))
                .ToArray();
            context.FinalizationSummaryFinalizers.AddRange(toSave);
        }
        await context.SaveChangesAsync();
        return block; 
    }
    
    private Block MapBlock(BlockInfo blockInfo, BlockSummary blockSummary, RewardStatus rewardStatus)
    {
        var block = new Block
        {
            BlockHash = blockInfo.BlockHash.AsString,
            BlockHeight = blockInfo.BlockHeight,
            BlockSlotTime = blockInfo.BlockSlotTime,
            BakerId = blockInfo.BlockBaker,
            Finalized = blockInfo.Finalized,
            TransactionCount = blockInfo.TransactionCount,
            SpecialEvents = new SpecialEvents
            {
                Mint = MapMint(blockSummary.SpecialEvents.OfType<MintSpecialEvent>().SingleOrDefault()),
                FinalizationRewards = MapFinalizationRewards(blockSummary.SpecialEvents.OfType<FinalizationRewardsSpecialEvent>().SingleOrDefault()),
                BlockRewards = MapBlockRewards(blockSummary.SpecialEvents.OfType<BlockRewardSpecialEvent>().SingleOrDefault()),
                BakingRewards = MapBakingRewards(blockSummary.SpecialEvents.OfType<BakingRewardsSpecialEvent>().SingleOrDefault()),
            },
            FinalizationSummary = MapFinalizationSummary(blockSummary.FinalizationData),
            BalanceStatistics = MapBalanceStatistics(rewardStatus),
            BlockStatistics = new BlockStatistics
            {
                BlockTime = GetBlockTime(blockInfo), 
                FinalizationTime = null // Updated when the block with proof of finalization for this block is imported
            }
        };
        return block;
    }

    private double GetBlockTime(BlockInfo blockInfo)
    {
        if (blockInfo.BlockHeight == 0) return 0;
        
        var previousBlockSlotTime = _previousBlockSlotTimeCache.GetCommittedValue();
        if (!previousBlockSlotTime.HasValue)
            throw new InvalidOperationException("Expected previous block slot time to have a value!");
        
        var blockTime = blockInfo.BlockSlotTime - previousBlockSlotTime.Value;
        return Math.Round(blockTime.TotalSeconds, 1);
    }

    private static BalanceStatistics MapBalanceStatistics(RewardStatus rewardStatus)
    {
        return new BalanceStatistics(
            rewardStatus.TotalAmount.MicroCcdValue, 
            rewardStatus.TotalEncryptedAmount.MicroCcdValue, 
            0, // Updated later in db-transaction, when amounts locked in schedules has been updated.
            rewardStatus.BakingRewardAccount.MicroCcdValue, 
            rewardStatus.FinalizationRewardAccount.MicroCcdValue, 
            rewardStatus.GasAccount.MicroCcdValue);
    }

    private Mint? MapMint(MintSpecialEvent? mint)
    {
        if (mint == null) return null;

        return new Mint
        {
            BakingReward = mint.MintBakingReward.MicroCcdValue,
            FinalizationReward = mint.MintFinalizationReward.MicroCcdValue,
            PlatformDevelopmentCharge = mint.MintPlatformDevelopmentCharge.MicroCcdValue,
            FoundationAccount = mint.FoundationAccount.AsString
        };
    }

    private FinalizationSummary? MapFinalizationSummary(FinalizationData? data)
    {
        if (data == null) return null;
        return new FinalizationSummary
        {
            FinalizedBlockHash = data.FinalizationBlockPointer.AsString,
            FinalizationIndex = data.FinalizationIndex,
            FinalizationDelay = data.FinalizationDelay,
        };
    }

    private BakingRewards? MapBakingRewards(BakingRewardsSpecialEvent? rewards)
    {
        if (rewards == null) return null;

        return new BakingRewards
        {
            Remainder = rewards.Remainder.MicroCcdValue,
        };
    }

    private BlockRewards? MapBlockRewards(BlockRewardSpecialEvent? rewards)
    {
        if (rewards == null) return null;

        return new BlockRewards
        {
            TransactionFees = rewards.TransactionFees.MicroCcdValue,
            OldGasAccount = rewards.OldGasAccount.MicroCcdValue,
            NewGasAccount = rewards.NewGasAccount.MicroCcdValue,
            BakerReward = rewards.BakerReward.MicroCcdValue,
            FoundationCharge = rewards.FoundationCharge.MicroCcdValue,
            BakerAccountAddress = rewards.Baker.AsString,
            FoundationAccountAddress = rewards.FoundationAccount.AsString
        };
    }

    private FinalizationRewards? MapFinalizationRewards(FinalizationRewardsSpecialEvent? rewards)
    {
        if (rewards == null) return null;

        return new FinalizationRewards
        {
            Remainder = rewards.Remainder.MicroCcdValue,
        };
    }
    
    private static BlockRelated<FinalizationReward> MapFinalizationReward(Block block, int index, AccountAddressAmount value)
    {
        return new BlockRelated<FinalizationReward>
        {
            BlockId = block.Id,
            Index = index,
            Entity = new FinalizationReward
            {
                Address = value.Address.AsString,
                Amount = value.Amount.MicroCcdValue
            }
        };
    }

    private static BlockRelated<BakingReward> MapBakingReward(Block block, int index, AccountAddressAmount value)
    {
        return new BlockRelated<BakingReward>
        {
            BlockId = block.Id,
            Index = index,
            Entity = new BakingReward()
            {
                Address = value.Address.AsString,
                Amount = value.Amount.MicroCcdValue
            }
        };
    }

    private static BlockRelated<FinalizationSummaryParty> MapFinalizer(Block block, int index, ConcordiumSdk.NodeApi.Types.FinalizationSummaryParty value)
    {
        return new BlockRelated<FinalizationSummaryParty>
        {
            BlockId = block.Id,
            Index = index,
            Entity = new FinalizationSummaryParty
            {
                BakerId = value.BakerId,
                Weight = value.Weight,
                Signed = value.Signed
            }
        };
    }

    public async Task CalculateAndUpdateTotalAmountLockedInSchedules(long blockId, DateTimeOffset blockSlotTime)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var conn = context.Database.GetDbConnection();
        
        var sql = "select sum(amount) from graphql_account_release_schedule where timestamp > @BlockSlotTime";
        var result = await conn.ExecuteScalarAsync<long>(sql, new { BlockSlotTime = blockSlotTime });
        
        var updateSql = @"
            update graphql_blocks 
            set bal_stats_total_amount_locked_in_schedules = @AmountLockedInSchedules 
            where id = @BlockId";
        
        await conn.ExecuteAsync(updateSql, new
        {
            AmountLockedInSchedules = result, 
            BlockId = blockId
        });
    }
    
    private async Task<DateTimeOffset?> InitializePreviousBlockSlotTime(BlockInfo blockInfo, GraphQlDbContext dbContext)
    {
        if (blockInfo.BlockHeight == 0)
            return null;

        var previousBlockHeight = blockInfo.BlockHeight - 1;
        var result = await dbContext.Blocks
            .Where(x => x.BlockHeight == previousBlockHeight)
            .Select(x => x.BlockSlotTime)
            .ToArrayAsync();
        
        if (result.Length == 0)
            throw new InvalidOperationException($"Could not find previous block in database [previous block height={previousBlockHeight}]");

        return result.Single();
    }

    public async Task UpdateFinalizationTimeOnBlocksInFinalizationProof(Block block)
    {
        if (block.FinalizationSummary == null) return;

        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var finalizedBlockHeights = await context.Blocks
            .Where(x => x.BlockHash == block.FinalizationSummary.FinalizedBlockHash)
            .Select(x => x.BlockHeight)
            .ToArrayAsync();

        if (finalizedBlockHeights.Length != 1) throw new InvalidOperationException();
        var maxBlockHeight = finalizedBlockHeights.Single();
        var minBlockHeight = _maxBlockHeightWithUpdatedFinalizationTimeCache.GetCommittedValue() ?? maxBlockHeight - 1;
        
        var result = await context.Blocks
            .Where(x => x.BlockHeight > minBlockHeight && x.BlockHeight <= maxBlockHeight)
            .Select(x => new
            {
                x.BlockHeight, 
                x.BlockSlotTime,
                FinalizationTimeSecs = Math.Round((block.BlockSlotTime - x.BlockSlotTime).TotalSeconds, 1)
            })
            .ToArrayAsync();

        var conn = context.Database.GetDbConnection();
        await conn.ExecuteAsync(
            "update graphql_blocks set block_stats_finalization_time_secs = @FinalizationTimeSecs where block_height = @BlockHeight",
            result);
        _maxBlockHeightWithUpdatedFinalizationTimeCache.EnqueueUpdate(maxBlockHeight);
    }
}