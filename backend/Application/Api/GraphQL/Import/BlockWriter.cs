using System.Threading.Tasks;
using Application.Api.GraphQL.Blocks;
using Application.Api.GraphQL.EfCore;
using Application.Common.Diagnostics;
using Concordium.Sdk.Types;
using Dapper;
using Microsoft.EntityFrameworkCore;
using AccountAddress = Application.Api.GraphQL.Accounts.AccountAddress;
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

        var block = MapBlock(
            blockInfo,
            finalizationSummary,
            rewardStatus,
            blockTime,
            chainParametersId,
            bakerUpdateResults,
            delegationUpdateResults,
            importState,
            nonCirculatingAccountsBalance);
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
        var specialEventsMapped = MapSpecialEvents(block.Id, specialEvents).ToArray();
        context.SpecialEvents.AddRange(specialEventsMapped);
        
        await context.SaveChangesAsync();
        return specialEventsMapped;
    }
    
    private static IEnumerable<Blocks.SpecialEvent> MapSpecialEvents(long blockId, IList<ISpecialEvent> inputs)
    {
        foreach (var input in inputs)
        {
            switch (input)
            {
                case BakingRewards bakingRewards:
                    var bakingRewardsAccountAmounts = bakingRewards.Rewards
                        .Select(kv => (AccountAddress: kv.Key, Amount: kv.Value))
                        .ToList();
                    yield return new Blocks.BakingRewardsSpecialEvent
                    {
                        BlockId = blockId,
                        Remainder = bakingRewards.Remainder.Value, 
                        AccountAddresses = bakingRewardsAccountAmounts
                            .Select(reward => new AccountAddress(reward.AccountAddress.ToString())).ToArray(),
                        Amounts = bakingRewardsAccountAmounts.Select(reward => reward.Amount.Value).ToArray()
                    };
                    break;
                case BlockAccrueReward blockAccrueReward:
                    yield return new Blocks.BlockAccrueRewardSpecialEvent
                    {
                        BlockId = blockId,
                        TransactionFees = blockAccrueReward.TransactionFees.Value,
                        OldGasAccount = blockAccrueReward.OldGasAccount.Value,
                        NewGasAccount = blockAccrueReward.NewGasAccount.Value,
                        BakerReward = blockAccrueReward.BakerReward.Value,
                        PassiveReward = blockAccrueReward.PassiveReward.Value,
                        FoundationCharge = blockAccrueReward.FoundationCharge.Value,
                        BakerId = blockAccrueReward.BakerId.Id.Index
                    };
                    break;
                case BlockReward blockReward:
                    yield return new Blocks.BlockRewardsSpecialEvent
                    {
                        BlockId = blockId,
                        TransactionFees = blockReward.TransactionFees.Value,
                        OldGasAccount = blockReward.OldGasAccount.Value,
                        NewGasAccount = blockReward.NewGasAccount.Value,
                        BakerReward = blockReward.BakerReward.Value,
                        FoundationCharge = blockReward.FoundationCharge.Value,
                        BakerAccountAddress = new AccountAddress(blockReward.Baker.ToString()),
                        FoundationAccountAddress = new AccountAddress(blockReward.FoundationAccount.ToString())
                    };
                    break;
                case FinalizationRewards finalizationRewards:
                    var accountAmounts = finalizationRewards.Rewards
                        .Select(kv => (AccountAddress: kv.Key, Amount: kv.Value))
                        .ToList();
                    yield return new Blocks.FinalizationRewardsSpecialEvent
                    {
                        BlockId = blockId,
                        Remainder = finalizationRewards.Remainder.Value,
                        AccountAddresses = accountAmounts
                            .Select(reward => new AccountAddress(reward.AccountAddress.ToString())).ToArray(),
                        Amounts = accountAmounts.Select(reward => reward.Amount.Value).ToArray()
                    };
                    break;
                case Mint mint:
                    yield return new Blocks.MintSpecialEvent
                    {
                        BlockId = blockId,
                        BakingReward = mint.MintBakingReward.Value,
                        FinalizationReward = mint.MintFinalizationReward.Value,
                        PlatformDevelopmentCharge = mint.MintPlatformDevelopmentCharge.Value,
                        FoundationAccountAddress = new AccountAddress(mint.FoundationAccount.ToString())
                    };
                    break;
                case PaydayAccountReward paydayAccountReward:
                    yield return new Blocks.PaydayAccountRewardSpecialEvent
                    {
                        BlockId = blockId,
                        Account = new AccountAddress(paydayAccountReward.Account.ToString()),
                        TransactionFees = paydayAccountReward.TransactionFees.Value,
                        BakerReward = paydayAccountReward.BakerReward.Value,
                        FinalizationReward = paydayAccountReward.FinalizationReward.Value
                    };
                    break;
                case PaydayFoundationReward paydayFoundationReward:
                    yield return new Blocks.PaydayFoundationRewardSpecialEvent
                    {
                        BlockId = blockId,
                        FoundationAccount = new AccountAddress(paydayFoundationReward.FoundationAccount.ToString()),
                        DevelopmentCharge = paydayFoundationReward.DevelopmentCharge.Value,
                    };
                    break;
                case PaydayPoolReward paydayPoolReward:
                    yield return new Blocks.PaydayPoolRewardSpecialEvent
                    {
                        BlockId = blockId,
                        Pool = paydayPoolReward.PoolOwner.HasValue
                            ? new BakerPoolRewardTarget((long)paydayPoolReward.PoolOwner.Value)
                            : new PassiveDelegationPoolRewardTarget(),
                        TransactionFees = paydayPoolReward.TransactionFees.Value,
                        BakerReward = paydayPoolReward.BakerReward.Value,
                        FinalizationReward = paydayPoolReward.FinalizationReward.Value
                    };
                    break;
            }
        }
    }

    private Block MapBlock(
        BlockInfo blockInfo,
        Concordium.Sdk.Types.FinalizationSummary? finalizationSummary,
        RewardOverviewBase rewardStatus,
        double blockTime,
        int chainParametersId,
        BakerUpdateResults bakerUpdateResults,
        DelegationUpdateResults delegationUpdateResults,
        ImportState importState,
        ulong nonCirculatingAccountsBalance)
    {
        var block = new Block
        {
            BlockHash = blockInfo.BlockHash.ToString(),
            BlockHeight = (int)blockInfo.BlockHeight,
            BlockSlotTime = blockInfo.BlockSlotTime,
            BakerId = blockInfo.BlockBaker != null ? (int)blockInfo.BlockBaker!.Value.Id.Index : null,
            Finalized = blockInfo.Finalized,
            TransactionCount = (int)blockInfo.TransactionCount,
            FinalizationSummary = MapFinalizationSummary(finalizationSummary),
            BalanceStatistics = MapBalanceStatistics(
                rewardStatus, 
                blockInfo.BlockSlotTime, 
                bakerUpdateResults, 
                delegationUpdateResults, 
                importState, 
                nonCirculatingAccountsBalance),
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

    private BalanceStatistics MapBalanceStatistics(
        RewardOverviewBase rewardStatus, 
        DateTimeOffset blockSlotTime,
        BakerUpdateResults bakerUpdateResults, 
        DelegationUpdateResults delegationUpdateResults, 
        ImportState importState, 
        ulong nonCirculatingAccountsBalance)
    {
        var totalCcdSupply = rewardStatus.TotalAmount.Value;
        var totalCirculatingCcdSupply = totalCcdSupply - nonCirculatingAccountsBalance;

        return new BalanceStatistics(
            totalCcdSupply,
            totalCirculatingCcdSupply,
            _changeCalculator.CalculateTotalAmountUnlocked(blockSlotTime, importState.GenesisBlockHash),
            rewardStatus.TotalEncryptedAmount.Value,
            0, // Updated later in db-transaction, when amounts locked in schedules has been updated.
            bakerUpdateResults.TotalAmountStaked + delegationUpdateResults.TotalAmountStaked,
            bakerUpdateResults.TotalAmountStaked,
            delegationUpdateResults.TotalAmountStaked,
            rewardStatus.BakingRewardAccount.Value, 
            rewardStatus.FinalizationRewardAccount.Value, 
            rewardStatus.GasAccount.Value);
    }

    private FinalizationSummary? MapFinalizationSummary(Concordium.Sdk.Types.FinalizationSummary? data)
    {
        if (data == null) return null;
        return new FinalizationSummary
        {
            FinalizedBlockHash = data.BlockPointer.ToString(),
            FinalizationIndex = (long)data.Index,
            FinalizationDelay = (long)data.Delay,
        };
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
