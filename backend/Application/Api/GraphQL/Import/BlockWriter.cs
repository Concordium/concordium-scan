using System.Threading.Tasks;
using Application.Api.GraphQL.EfCore;
using Application.Common.Diagnostics;
using ConcordiumSdk.NodeApi.Types;
using Dapper;
using Microsoft.EntityFrameworkCore;

namespace Application.Api.GraphQL.Import;

public class BlockWriter
{
    private readonly IDbContextFactory<GraphQlDbContext> _dbContextFactory;
    private readonly IMetrics _metrics;

    public BlockWriter(IDbContextFactory<GraphQlDbContext> dbContextFactory, IMetrics metrics)
    {
        _dbContextFactory = dbContextFactory;
        _metrics = metrics;
    }

    public async Task<Block> AddBlock(BlockInfo blockInfo, BlockSummary blockSummary, RewardStatus rewardStatus, int chainParametersId, ImportState importState)
    {
        using var counter = _metrics.MeasureDuration(nameof(BlockWriter), nameof(AddBlock));
        
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        var blockTime = GetBlockTime(blockInfo, importState.LastBlockSlotTime);
        importState.LastBlockSlotTime = blockInfo.BlockSlotTime;

        var block = MapBlock(blockInfo, blockSummary, rewardStatus, blockTime, chainParametersId);
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
    
    private Block MapBlock(BlockInfo blockInfo, BlockSummary blockSummary, RewardStatus rewardStatus, double blockTime, int chainParametersId)
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
                BlockTime = blockTime, 
                FinalizationTime = null // Updated when the block with proof of finalization for this block is imported
            },
            ChainParametersId = chainParametersId
        };
        return block;
    }

    private double GetBlockTime(BlockInfo blockInfo, DateTimeOffset previousBlockSlotTime)
    {
        var blockTime = blockInfo.BlockSlotTime - previousBlockSlotTime;
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

    public async Task UpdateTotalAmountLockedInReleaseSchedules(Block block)
    {
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