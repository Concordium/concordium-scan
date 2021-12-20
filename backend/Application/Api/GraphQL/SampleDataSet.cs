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

        var result =
            conn.Query(
                "SELECT id, block_hash, block_height, block_slot_time, block_baker, transaction_count FROM block WHERE block_height < 40000");
        
        return result.Select(obj => new Block()
        {
            Id = obj.id,
            BlockHash = new BlockHash((byte[])obj.block_hash).AsString,
            BlockHeight = (int)obj.block_height,
            BlockSlotTime = (DateTimeOffset)obj.block_slot_time,
            BakerId = obj.block_baker,
            Finalized = true,
            TransactionCount = (int)obj.transaction_count
        }).ToArray();
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