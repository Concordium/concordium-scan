using Application.Database;
using ConcordiumSdk.Types;
using Dapper;
using HotChocolate;
using HotChocolate.Types;
using Npgsql;

namespace Application.Api.GraphQL;

public class Block
{
    public string BlockHash { get; set; }
    public int BlockHeight { get; set; }
    public DateTimeOffset BlockSlotTime { get; init; }
    public bool Finalized { get; init; }
    public int TransactionCount { get; init; }

    [UsePaging]
    public IEnumerable<Transaction> GetTransactions([Service] DatabaseSettings dbSettings)
    {
        using var conn = new NpgsqlConnection(dbSettings.ConnectionString);
        conn.Open();

        var result =
            conn.Query(
                "SELECT transaction_index, transaction_hash, sender, cost, energy_cost FROM transaction_summary WHERE block_height = " + BlockHeight);

        return result.Select(obj => new Transaction()
        {
            TransactionIndex = (int)obj.transaction_index,
            TransactionHash = new TransactionHash((byte[])obj.transaction_hash).AsString,
            SenderAccountAddress = new AccountAddress((byte[])obj.sender).AsString,
            CcdCost = (int)obj.cost,
            EnergyCost = (int)obj.energy_cost
        });
    }
}