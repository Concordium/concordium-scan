using System.Threading.Tasks;
using Application.Api.GraphQL.Blocks;
using Application.Api.GraphQL.EfCore;
using Application.Common.Diagnostics;
using ConcordiumSdk.NodeApi.Types;
using Dapper;
using Microsoft.EntityFrameworkCore;
using AccountAddress = Application.Api.GraphQL.Accounts.AccountAddress;
using BakingRewardsSpecialEvent = ConcordiumSdk.NodeApi.Types.BakingRewardsSpecialEvent;
using BlockAccrueRewardSpecialEvent = ConcordiumSdk.NodeApi.Types.BlockAccrueRewardSpecialEvent;
using FinalizationRewardsSpecialEvent = ConcordiumSdk.NodeApi.Types.FinalizationRewardsSpecialEvent;
using FinalizationSummaryParty = Application.Api.GraphQL.Blocks.FinalizationSummaryParty;
using MintSpecialEvent = ConcordiumSdk.NodeApi.Types.MintSpecialEvent;
using PaydayAccountRewardSpecialEvent = ConcordiumSdk.NodeApi.Types.PaydayAccountRewardSpecialEvent;
using PaydayFoundationRewardSpecialEvent = ConcordiumSdk.NodeApi.Types.PaydayFoundationRewardSpecialEvent;
using PaydayPoolRewardSpecialEvent = ConcordiumSdk.NodeApi.Types.PaydayPoolRewardSpecialEvent;
using SpecialEvent = ConcordiumSdk.NodeApi.Types.SpecialEvent;

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

    public async Task<Block> AddBlock(BlockInfo blockInfo, BlockSummaryBase blockSummary, RewardStatusBase rewardStatus,
        int chainParametersId, BakerUpdateResults bakerUpdateResults, DelegationUpdateResults delegationUpdateResults,
        ImportState importState)
    {
        using var counter = _metrics.MeasureDuration(nameof(BlockWriter), nameof(AddBlock));
        
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        var blockTime = GetBlockTime(blockInfo, importState.LastBlockSlotTime);
        importState.LastBlockSlotTime = blockInfo.BlockSlotTime;

        var block = MapBlock(blockInfo, blockSummary, rewardStatus, blockTime, chainParametersId, bakerUpdateResults, delegationUpdateResults, importState);
        context.Blocks.Add(block);
        
        await context.SaveChangesAsync(); // assign ID to block!

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

    public async Task<Blocks.SpecialEvent[]> AddSpecialEvents(Block block, BlockSummaryBase blockSummary)
    {
        using var counter = _metrics.MeasureDuration(nameof(BlockWriter), nameof(AddSpecialEvents));
        
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var specialEvents = MapSpecialEvents(block.Id, blockSummary.SpecialEvents).ToArray();
        context.SpecialEvents.AddRange(specialEvents);
        
        await context.SaveChangesAsync();
        return specialEvents;
    }
    
    private IEnumerable<Blocks.SpecialEvent> MapSpecialEvents(long blockId, SpecialEvent[] inputs)
    {
        foreach (var input in inputs)
        {
            Blocks.SpecialEvent result = input switch
            {
                MintSpecialEvent x => new Blocks.MintSpecialEvent
                {
                    BlockId = blockId,
                    BakingReward = x.MintBakingReward.MicroCcdValue,
                    FinalizationReward = x.MintFinalizationReward.MicroCcdValue,
                    PlatformDevelopmentCharge = x.MintPlatformDevelopmentCharge.MicroCcdValue,
                    FoundationAccountAddress  = new AccountAddress(x.FoundationAccount.AsString)
                },
                FinalizationRewardsSpecialEvent x => new Blocks.FinalizationRewardsSpecialEvent
                {
                    BlockId = blockId,
                    Remainder = x.Remainder.MicroCcdValue, 
                    AccountAddresses = x.FinalizationRewards.Select(reward => new AccountAddress(reward.Address.AsString)).ToArray(), 
                    Amounts = x.FinalizationRewards.Select(reward => reward.Amount.MicroCcdValue).ToArray() 
                },
                BlockRewardSpecialEvent x => new Blocks.BlockRewardsSpecialEvent
                {
                    BlockId = blockId,
                    TransactionFees = x.TransactionFees.MicroCcdValue,
                    OldGasAccount = x.OldGasAccount.MicroCcdValue,
                    NewGasAccount = x.NewGasAccount.MicroCcdValue,
                    BakerReward = x.BakerReward.MicroCcdValue,
                    FoundationCharge = x.FoundationCharge.MicroCcdValue,
                    BakerAccountAddress = new AccountAddress(x.Baker.AsString),
                    FoundationAccountAddress = new AccountAddress(x.FoundationAccount.AsString)
                },
                BakingRewardsSpecialEvent x => new Blocks.BakingRewardsSpecialEvent
                {
                    BlockId = blockId,
                    Remainder = x.Remainder.MicroCcdValue, 
                    AccountAddresses = x.BakerRewards.Select(reward => new AccountAddress(reward.Address.AsString)).ToArray(), 
                    Amounts = x.BakerRewards.Select(reward => reward.Amount.MicroCcdValue).ToArray() 
                },
                PaydayAccountRewardSpecialEvent x => new Blocks.PaydayAccountRewardSpecialEvent
                {
                    BlockId = blockId,
                    Account = new AccountAddress(x.Account.AsString),
                    TransactionFees = x.TransactionFees.MicroCcdValue,
                    BakerReward = x.BakerReward.MicroCcdValue,
                    FinalizationReward = x.FinalizationReward.MicroCcdValue
                },
                BlockAccrueRewardSpecialEvent x => new Blocks.BlockAccrueRewardSpecialEvent
                {
                    BlockId = blockId,
                    TransactionFees = x.TransactionFees.MicroCcdValue,
                    OldGasAccount = x.OldGasAccount.MicroCcdValue,
                    NewGasAccount = x.NewGasAccount.MicroCcdValue,
                    BakerReward = x.BakerReward.MicroCcdValue,
                    PassiveReward = x.PassiveReward.MicroCcdValue,
                    FoundationCharge = x.FoundationCharge.MicroCcdValue,
                    BakerId = x.BakerId
                },
                PaydayFoundationRewardSpecialEvent x => new Blocks.PaydayFoundationRewardSpecialEvent
                {
                    BlockId = blockId,
                    FoundationAccount = new AccountAddress(x.FoundationAccount.AsString),
                    DevelopmentCharge = x.DevelopmentCharge.MicroCcdValue,
                },
                PaydayPoolRewardSpecialEvent x => new Blocks.PaydayPoolRewardSpecialEvent
                {
                    BlockId = blockId,
                    Pool = x.PoolOwner.HasValue ? new BakerPoolRewardTarget((long)x.PoolOwner.Value) : new PassiveDelegationPoolRewardTarget(),
                    TransactionFees = x.TransactionFees.MicroCcdValue,
                    BakerReward = x.BakerReward.MicroCcdValue,
                    FinalizationReward = x.FinalizationReward.MicroCcdValue
                },
                _ => throw new NotImplementedException()
            };
            yield return result;
        }
    }

    private Block MapBlock(BlockInfo blockInfo, BlockSummaryBase blockSummary, RewardStatusBase rewardStatus,
        double blockTime, int chainParametersId, BakerUpdateResults bakerUpdateResults, 
        DelegationUpdateResults delegationUpdateResults, ImportState importState)
    {
        var block = new Block
        {
            BlockHash = blockInfo.BlockHash.AsString,
            BlockHeight = blockInfo.BlockHeight,
            BlockSlotTime = blockInfo.BlockSlotTime,
            BakerId = blockInfo.BlockBaker,
            Finalized = blockInfo.Finalized,
            TransactionCount = blockInfo.TransactionCount,
            FinalizationSummary = MapFinalizationSummary(blockSummary.FinalizationData),
            BalanceStatistics = MapBalanceStatistics(rewardStatus, blockInfo.BlockSlotTime, bakerUpdateResults, delegationUpdateResults, importState),
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

    private BalanceStatistics MapBalanceStatistics(RewardStatusBase rewardStatus, DateTimeOffset blockSlotTime,
        BakerUpdateResults bakerUpdateResults, DelegationUpdateResults delegationUpdateResults, ImportState importState)
    {
        return new BalanceStatistics(
            rewardStatus.TotalAmount.MicroCcdValue,
            _changeCalculator.CalculateTotalAmountReleased(blockSlotTime, importState.GenesisBlockHash),
            rewardStatus.TotalEncryptedAmount.MicroCcdValue, 
            0, // Updated later in db-transaction, when amounts locked in schedules has been updated.
            bakerUpdateResults.TotalAmountStaked + delegationUpdateResults.TotalAmountStaked,
            bakerUpdateResults.TotalAmountStaked,
            delegationUpdateResults.TotalAmountStaked,
            rewardStatus.BakingRewardAccount.MicroCcdValue, 
            rewardStatus.FinalizationRewardAccount.MicroCcdValue, 
            rewardStatus.GasAccount.MicroCcdValue);
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