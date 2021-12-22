using Application.Database;
using ConcordiumSdk.Types;
using Dapper;
using Npgsql;

namespace Application.Api.GraphQL;

public class SampleDataSet
{
    private readonly DatabaseSettings _dbSettings;
    private readonly Lazy<Block[]> _allBlocks; 
    private readonly Lazy<Transaction[]> _allTransactions;

    public SampleDataSet(DatabaseSettings dbSettings)
    {
        _dbSettings = dbSettings;
        _allBlocks = new Lazy<Block[]>(FetchSampleBlockSetFromDb);
        _allTransactions = new Lazy<Transaction[]>(FetchSampleTransactionSetFromDb);
    }

    public Block[] AllBlocks => _allBlocks.Value;
    public Transaction[] AllTransactions => _allTransactions.Value;
    
    private Block[] FetchSampleBlockSetFromDb()
    {
        using var conn = new NpgsqlConnection(_dbSettings.ConnectionString);
        conn.Open();

        var finalizationRewards = conn
            .Query("select block_id, index, address, amount from finalization_reward where block_id < 40000")
            .ToArray();
        
        var bakingRewards = conn
            .Query("select block_id, index, address, amount from baking_reward where block_id < 40000")
            .ToArray();
        
        var result =
            conn.Query(
                "SELECT id, block_hash, block_height, block_slot_time, block_baker, transaction_count, mint_baking_reward, mint_finalization_reward, mint_platform_development_charge, mint_foundation_account, block_reward_transaction_fees, block_reward_old_gas_account, block_reward_new_gas_account, block_reward_baker_reward, block_reward_foundation_charge, block_reward_baker_address, block_reward_foundation_account, finalization_reward_remainder, baking_reward_remainder FROM block WHERE block_height < 40000");
        
        return result.Select(obj => new Block()
        {
            Id = obj.id,
            BlockHash = new BlockHash((byte[])obj.block_hash).AsString,
            BlockHeight = (int)obj.block_height,
            BlockSlotTime = (DateTimeOffset)obj.block_slot_time,
            BakerId = obj.block_baker,
            Finalized = true,
            TransactionCount = (int)obj.transaction_count,
            SpecialEvents = new SpecialEvents
            {
                Mint = MapMinting(obj),
                BlockRewards = MapBlockRewards(obj),
                FinalizationRewards = MapFinalizationRewards(obj, finalizationRewards),
                BakingRewards = MapBakingRewards(obj, bakingRewards)
            }
        }).ToArray();
    }

    private BakingRewards? MapBakingRewards(dynamic obj, dynamic[] allBakingRewards)
    {
        if (obj.baking_reward_remainder == null)
            return null;

        var rewards = allBakingRewards
            .Where(x => x.block_id == obj.id)
            .OrderBy(x => x.index)
            .Select(x => new BakingReward
            {
                Amount = x.amount,
                Address = new AccountAddress((byte[])x.address).AsString
            })
            .ToArray();

        return new BakingRewards
        {
            Remainder = obj.baking_reward_remainder,
            Rewards = rewards
        };
    }

    private FinalizationRewards MapFinalizationRewards(dynamic obj, dynamic[] allFinalizationRewards)
    {
        if (obj.finalization_reward_remainder == null)
            return null;

        var rewards = allFinalizationRewards
            .Where(x => x.block_id == obj.id)
            .OrderBy(x => x.index)
            .Select(MapFinalizationReward)
            .ToArray();

        return new FinalizationRewards
        {
            Remainder = obj.finalization_reward_remainder,
            Rewards = rewards
        };
    }

    private FinalizationReward MapFinalizationReward(dynamic obj)
    {
        return new FinalizationReward
        {
            Amount = obj.amount,
            Address = new AccountAddress((byte[])obj.address).AsString
        };
    }

    private BlockRewards? MapBlockRewards(dynamic obj)
    {
        if (obj.block_reward_transaction_fees == null)
            return null;

        return new BlockRewards
        {
            TransactionFees = obj.block_reward_transaction_fees,
            OldGasAccount = obj.block_reward_old_gas_account,
            NewGasAccount = obj.block_reward_new_gas_account,
            BakerReward = obj.block_reward_baker_reward,
            FoundationCharge = obj.block_reward_foundation_charge,
            BakerAccountAddress = new AccountAddress((byte[])obj.block_reward_baker_address).AsString,
            FoundationAccountAddress = new AccountAddress((byte[])obj.block_reward_foundation_account).AsString
        };
    }

    private Mint? MapMinting(dynamic obj)
    {
        if (obj.mint_baking_reward == null)
            return null;

        return new Mint()
        {
            BakingReward = obj.mint_baking_reward,
            FinalizationReward = obj.mint_finalization_reward,
            PlatformDevelopmentCharge = obj.mint_platform_development_charge,
            FoundationAccount = new AccountAddress((byte[])obj.mint_foundation_account).AsString
        };
    }

    private Transaction[] FetchSampleTransactionSetFromDb()
    {
        using var conn = new NpgsqlConnection(_dbSettings.ConnectionString);
        conn.Open();

        var result =
            conn.Query(
                "SELECT id, block_height, block_hash, transaction_index, transaction_hash, sender, cost, energy_cost FROM transaction_summary WHERE block_height < 40000");
        
        return result.Select(obj => new Transaction()
        {
            Id = obj.id,
            BlockHeight = (int)obj.block_height,
            BlockHash = new BlockHash((byte[])obj.block_hash).AsString,
            TransactionIndex = obj.transaction_index,
            TransactionHash = new TransactionHash((byte[])obj.transaction_hash).AsString,
            SenderAccountAddress = obj.sender != null ? new AccountAddress((byte[])obj.sender).AsString : "",
            CcdCost = obj.cost,
            EnergyCost = obj.energy_cost
        }).ToArray();
    }

}