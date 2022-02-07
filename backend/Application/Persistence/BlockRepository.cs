using System.Data;
using System.Text.Json;
using Application.Database;
using ConcordiumSdk.NodeApi;
using ConcordiumSdk.NodeApi.Types;
using ConcordiumSdk.Types;
using Dapper;
using Npgsql;

namespace Application.Persistence;

public class BlockRepository
{
    private readonly DatabaseSettings _settings;
    private readonly JsonSerializerOptions _successEventsSerializerOptions;

    public BlockRepository(DatabaseSettings settings)
    {
        _settings = settings;
        _successEventsSerializerOptions = GrpcNodeJsonSerializerOptionsFactory.Create();
    }

    public int? GetMaxBlockHeight()
    {
        using var conn = new NpgsqlConnection(_settings.ConnectionString);
        conn.Open();

        var data = conn.QuerySingleOrDefault("SELECT block_height FROM block order by id desc limit 1");
        if (data == null) return null; 
        return (int)data.block_height;
    }

    public void Insert(BlockInfo blockInfo, string blockSummaryString, BlockSummary blockSummary)
    {
        using var conn = new NpgsqlConnection(_settings.ConnectionString);
        conn.Open();

        using var tx = conn.BeginTransaction(IsolationLevel.ReadCommitted);

        var mint = blockSummary.SpecialEvents?.OfType<MintSpecialEvent>().SingleOrDefault();
        var blockReward = blockSummary.SpecialEvents?.OfType<BlockRewardSpecialEvent>().SingleOrDefault();
        var finalizationRewards = blockSummary.SpecialEvents?.OfType<FinalizationRewardsSpecialEvent>().SingleOrDefault();
        var bakingRewards = blockSummary.SpecialEvents?.OfType<BakingRewardsSpecialEvent>().SingleOrDefault();
        
        var blockParams = new
        {
            Blockhash = blockInfo.BlockHash.AsBytes,
            Parentblock = blockInfo.BlockParent.AsBytes,
            Blocklastfinalized = blockInfo.BlockLastFinalized.AsBytes,
            Blockheight = blockInfo.BlockHeight,
            Genesisindex = blockInfo.GenesisIndex,
            Erablockheight = blockInfo.EraBlockHeight,
            Blockreceivetime = blockInfo.BlockReceiveTime,
            Blockarrivetime = blockInfo.BlockArriveTime,
            Blockslot = blockInfo.BlockSlot,
            Blockslottime = blockInfo.BlockSlotTime,
            Blockbaker = blockInfo.BlockBaker,
            Finalized = blockInfo.Finalized,
            Transactioncount = blockInfo.TransactionCount,
            Transactionenergycost = blockInfo.TransactionEnergyCost,
            Transactionsize = blockInfo.TransactionSize,
            Blockstatehash = new BlockHash(blockInfo.BlockStateHash).AsBytes,
            Blocksummary = blockSummaryString,
            MintBakingReward = mint != null ? Convert.ToInt64(mint.MintBakingReward.MicroCcdValue) : (long?)null,
            MintFinalizationReward = mint != null ? Convert.ToInt64(mint.MintFinalizationReward.MicroCcdValue) : (long?)null,
            MintPlatformDevelopmentCharge = mint != null ? Convert.ToInt64(mint.MintPlatformDevelopmentCharge.MicroCcdValue) : (long?)null,
            MintFoundationAccount = mint?.FoundationAccount.AsBytes,
            BlockRewardTransactionFees = blockReward != null ? Convert.ToInt64(blockReward.TransactionFees.MicroCcdValue) : (long?)null,
            BlockRewardOldGasAccount = blockReward != null ? Convert.ToInt64(blockReward.OldGasAccount.MicroCcdValue) : (long?)null,
            BlockRewardNewGasAccount = blockReward != null ? Convert.ToInt64(blockReward.NewGasAccount.MicroCcdValue) : (long?)null,
            BlockRewardBakerReward = blockReward != null ? Convert.ToInt64(blockReward.BakerReward.MicroCcdValue) : (long?)null,
            BlockRewardFoundationCharge = blockReward != null ? Convert.ToInt64(blockReward.FoundationCharge.MicroCcdValue) : (long?)null,
            BlockRewardBakerAddress = blockReward?.Baker.AsBytes,
            BlockRewardFoundationAccount = blockReward?.FoundationAccount.AsBytes,
            FinalizationRewardRemainder = finalizationRewards != null ? Convert.ToInt64(finalizationRewards.Remainder.MicroCcdValue) : (long?)null,
            BakingRewardRemainder = bakingRewards != null ? Convert.ToInt64(bakingRewards.Remainder.MicroCcdValue) : (long?)null,
            FinalizationDataBlockPointer = blockSummary.FinalizationData?.FinalizationBlockPointer.AsBytes,
            FinalizationDataIndex = blockSummary.FinalizationData?.FinalizationIndex,
            FinalizationDataDelay = blockSummary.FinalizationData?.FinalizationDelay
        };
        
        var blockId = conn.ExecuteScalar<long>(
            "INSERT INTO block(block_height, block_hash, parent_block, block_last_finalized, genesis_index, era_block_height, block_receive_time," +
            " block_arrive_time, block_slot, block_slot_time, block_baker, finalized, transaction_count, transaction_energy_cost, transaction_size," +
            " block_state_hash, block_summary, mint_baking_reward, mint_finalization_reward, mint_platform_development_charge, mint_foundation_account, "+
            " block_reward_transaction_fees, block_reward_old_gas_account, block_reward_new_gas_account, block_reward_baker_reward, block_reward_foundation_charge, block_reward_baker_address, block_reward_foundation_account,"+
            " finalization_reward_remainder, baking_reward_remainder, finalization_data_block_pointer, finalization_data_index, finalization_data_delay) " +
            " VALUES (@Blockheight, @Blockhash, @Parentblock, @Blocklastfinalized, @Genesisindex, @Erablockheight, @Blockreceivetime," +
            " @Blockarrivetime, @Blockslot, @Blockslottime, @Blockbaker, @Finalized, @Transactioncount, @Transactionenergycost, @Transactionsize," +
            " @Blockstatehash, CAST(@Blocksummary AS json), @MintBakingReward, @MintFinalizationReward, @MintPlatformDevelopmentCharge, @MintFoundationAccount," +
            " @BlockRewardTransactionFees, @BlockRewardOldGasAccount, @BlockRewardNewGasAccount, @BlockRewardBakerReward, @BlockRewardFoundationCharge, @BlockRewardBakerAddress, @BlockRewardFoundationAccount," +
            " @FinalizationRewardRemainder, @BakingRewardRemainder, @FinalizationDataBlockPointer, @FinalizationDataIndex, @FinalizationDataDelay) returning id",
            blockParams);

        var transactionSummaries = blockSummary.TransactionSummaries.Select(tx => new
        {
            BlockId = blockId,
            BlockHeight = blockInfo.BlockHeight,
            BlockHash = blockInfo.BlockHash.AsBytes, 
            TransactionIndex = tx.Index,
            Sender = tx.Sender?.AsBytes,
            TransactionHash = tx.Hash.AsBytes,
            Cost = Convert.ToInt64(tx.Cost.MicroCcdValue),
            EnergyCost = tx.EnergyCost,
            TransactionType = MapTransactionType(tx.Type),
            TransactionSubType = MapTransactionSubType(tx.Type), 
            SuccessEvents = MapSuccessEvents(tx.Result),
            RejectReasonType = MapRejectReasonType(tx.Result)
        });
        
        conn.Execute(
            "INSERT INTO transaction_summary(block_id, block_height, block_hash, transaction_index, sender, transaction_hash, cost, energy_cost, transaction_type, transaction_sub_type, success_events, reject_reason_type) " +
            "VALUES (@BlockId, @BlockHeight, @BlockHash, @TransactionIndex, @Sender, @TransactionHash, @Cost, @EnergyCost, @TransactionType, @TransactionSubType, CAST(@SuccessEvents AS json), @RejectReasonType)",
            transactionSummaries);

        if (finalizationRewards != null)
        {
            var finalizationParams = finalizationRewards.FinalizationRewards.Select((reward, ix) => new
            {
                BlockId = blockId,
                Index = ix,
                Amount = Convert.ToInt64(reward.Amount.MicroCcdValue),
                Address = reward.Address.AsBytes
            });
            
            conn.Execute(
                "INSERT INTO finalization_reward(block_id, index, amount, address) " +
                "VALUES (@BlockId, @Index, @Amount, @Address)",
                finalizationParams);
        }
        
        if (bakingRewards != null)
        {
            var bakingParams = bakingRewards.BakerRewards.Select((reward, ix) => new
            {
                BlockId = blockId,
                Index = ix,
                Amount = Convert.ToInt64(reward.Amount.MicroCcdValue),
                Address = reward.Address.AsBytes
            });
            
            conn.Execute(
                "INSERT INTO baking_reward(block_id, index, amount, address) " +
                "VALUES (@BlockId, @Index, @Amount, @Address)",
                bakingParams);
        }

        if (blockSummary.FinalizationData != null)
        {
            var finalizersParams = blockSummary.FinalizationData.Finalizers.Select((finalizer, ix) => new
            {
                BlockId = blockId,
                Index = ix,
                BakerId = Convert.ToInt64(finalizer.BakerId),
                Weight = Convert.ToInt64(finalizer.Weight),
                finalizer.Signed,
            });
            
            conn.Execute(
                "INSERT INTO finalization_data_finalizers(block_id, index, baker_id, weight, signed) " +
                "VALUES (@BlockId, @Index, @BakerId, @Weight, @Signed)",
                finalizersParams);
        }
        
        tx.Commit();
    }

    private string? MapSuccessEvents(TransactionResult result)
    {
        if (result is TransactionSuccessResult success)
            return JsonSerializer.Serialize(success.Events, _successEventsSerializerOptions);

        return null;
    }

    private string? MapRejectReasonType(TransactionResult result)
    {
        if (result is TransactionRejectResult reject) return reject.Reason.GetType().Name;
        return null;
    }

    private int? MapTransactionSubType(TransactionType type)
    {
        return type.Type != null ? (int)type.Type : null;
    }

    private int MapTransactionType(TransactionType type)
    {
        return (int)type.Kind;
    }

    public BlockHash? GetGenesisBlockHash()
    {
        using var conn = new NpgsqlConnection(_settings.ConnectionString);
        conn.Open();

        var data = conn.QuerySingleOrDefault("SELECT block_height, block_hash FROM block order by id limit 1");
        if (data == null) return null;
        if (data.block_height != 0) throw new InvalidOperationException("Did not get the genesis block - unexpected!");
        var result = data != null ? new BlockHash((byte[])data.block_hash) : null;
        return result;
    }
}