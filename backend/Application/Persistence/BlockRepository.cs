using System.Data;
using System.Text.Json;
using Application.Database;
using ConcordiumSdk.NodeApi.Types;
using ConcordiumSdk.Types;
using Dapper;
using Npgsql;

namespace Application.Persistence;

public class BlockRepository
{
    private readonly DatabaseSettings _settings;

    public BlockRepository(DatabaseSettings settings)
    {
        _settings = settings;
    }

    public int? GetMaxBlockHeight()
    {
        using var conn = new NpgsqlConnection(_settings.ConnectionString);
        conn.Open();

        return conn.QuerySingle<int?>("SELECT max(block_height) FROM finalized_block");
    }

    public void Insert(BlockInfo blockInfo, string blockSummaryString, BlockSummary blockSummary)
    {
        using var conn = new NpgsqlConnection(_settings.ConnectionString);
        conn.Open();

        using var tx = conn.BeginTransaction(IsolationLevel.ReadCommitted);

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
            Blocksummary = blockSummaryString
        };

        conn.Execute(
            "INSERT INTO finalized_block(block_height, block_hash, parent_block, block_last_finalized, genesis_index, era_block_height, block_receive_time, block_arrive_time, block_slot, block_slot_time, block_baker, transaction_count, transaction_energy_cost, transaction_size, block_state_hash, block_summary) " +
            " VALUES (@Blockheight, @Blockhash, @Parentblock, @Blocklastfinalized, @Genesisindex, @Erablockheight, @Blockreceivetime, @Blockarrivetime, @Blockslot, @Blockslottime, @Blockbaker, @Transactioncount, @Transactionenergycost, @Transactionsize, @Blockstatehash, CAST(@Blocksummary AS json))",
            blockParams);

        var transactionSummaries = blockSummary.TransactionSummaries.Select(tx => new
        {
            BlockHeight = blockInfo.BlockHeight,
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
            "INSERT INTO transaction_summary(block_height, transaction_index, sender, transaction_hash, cost, energy_cost, transaction_type, transaction_sub_type, success_events, reject_reason_type) " +
            "VALUES (@BlockHeight, @TransactionIndex, @Sender, @TransactionHash, @Cost, @EnergyCost, @TransactionType, @TransactionSubType, CAST(@SuccessEvents AS json), @RejectReasonType)",
            transactionSummaries);
        
        tx.Commit();
    }

    private string? MapSuccessEvents(TransactionResult result)
    {
        if (result is TransactionSuccessResult success)
            return success.Events.ToString();

        return null;
    }

    private string? MapRejectReasonType(TransactionResult result)
    {
        if (result is TransactionRejectResult reject) return reject.Tag;
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

    public TransactionSummary[] FindTransactionSummaries(DateTimeOffset startTime, DateTimeOffset endTime, params TransactionType[] types)
    {
        if (types.Length == 0)
            return Array.Empty<TransactionSummary>();
        
        using var conn = new NpgsqlConnection(_settings.ConnectionString);
        conn.Open();

        var boundsParams = new { startTime, endTime };
        var boundsResult = conn.QuerySingle(
            "select (select min(block_height) from finalized_block where block_slot_time >= @startTime) as start_block_height, (select max(block_height) from finalized_block where block_slot_time <= @endTime) as end_block_height", boundsParams);

        if (boundsResult.start_block_height == null || 
            boundsResult.end_block_height == null ||
            boundsResult.end_block_height < boundsResult.start_block_height)
            return new TransactionSummary[0];

        var queryParams = new
        {
            StartBlockHeight = boundsResult.start_block_height,
            EndBlockHeight = boundsResult.end_block_height
        };
        var dd = string.Join(" or ",
            types.Select(x =>
                $"(transaction_type = {MapTransactionType(x)} and transaction_sub_type = {MapTransactionSubType(x)})"));
        var sql = $"select transaction_index, sender, transaction_hash, cost, energy_cost, transaction_type, transaction_sub_type, success_events, reject_reason_type "+
            $"from transaction_summary where block_height >= @StartBlockHeight and block_height <= @EndBlockHeight and ({dd})";
        
        var transactionSummaries = conn.Query(sql, queryParams);

        return transactionSummaries
            .Select(x => new TransactionSummary(
                MapToAccountAddress(x.sender),
                new TransactionHash(x.transaction_hash),
                CcdAmount.FromMicroCcd(Convert.ToUInt64(x.cost)),
                Convert.ToInt32(x.energy_cost),
                MapToType(x),
                MapToResult(x),
                x.transaction_index))
            .ToArray();
    }

    private AccountAddress? MapToAccountAddress(dynamic obj)
    {
        if (obj == null) return null;
        return new AccountAddress((byte[])obj);
    }

    private TransactionResult MapToResult(dynamic obj)
    {
        if (obj.success_events != null)
            return new TransactionSuccessResult { Events = JsonDocument.Parse(obj.success_events).RootElement };
        if (obj.reject_reason_type != null)
            return new TransactionRejectResult { Tag = obj.reject_reason_type };
        throw new InvalidOperationException("Unknown transaction result");
    }

    private TransactionType MapToType(dynamic obj)
    {
        if (obj.transaction_type == (int)BlockItemKind.AccountTransactionKind)
            return TransactionType.Get((AccountTransactionType)obj.transaction_sub_type);
        if (obj.transaction_type == (int)BlockItemKind.CredentialDeploymentKind)
            return TransactionType.Get((CredentialDeploymentTransactionType)obj.transaction_sub_type);
        if (obj.transaction_type == (int)BlockItemKind.UpdateInstructionKind)
            return TransactionType.Get((UpdateTransactionType)obj.transaction_sub_type);
        throw new InvalidOperationException("Unknown transaction summary type");
    }
}