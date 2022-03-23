using System.Threading.Tasks;
using Application.Api.GraphQL.EfCore;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Application.Api.GraphQL.Import;

public class ActualAccountWriter
{
    private readonly IDbContextFactory<GraphQlDbContext> _dbContextFactory;

    public ActualAccountWriter(IDbContextFactory<GraphQlDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task UpdateAccounts(IEnumerable<AccountWriter.AccountUpdate> accountUpdates)
    {
        var sql = @"
            UPDATE graphql_accounts 
            SET ccd_amount = ccd_amount + @AmountAdjustment,
                transaction_count = transaction_count + @TransactionsAdded
            WHERE id = @AccountId";

        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var connection = context.Database.GetDbConnection();

        await connection.OpenAsync();

        var batch = connection.CreateBatch();
        foreach (var accountUpdate in accountUpdates)
        {
            var cmd = batch.CreateBatchCommand();
            cmd.CommandText = sql;
            cmd.Parameters.Add(new NpgsqlParameter<long>("AccountId", accountUpdate.AccountId));
            cmd.Parameters.Add(new NpgsqlParameter<long>("AmountAdjustment", accountUpdate.AmountAdjustment));
            cmd.Parameters.Add(new NpgsqlParameter<int>("TransactionsAdded", accountUpdate.TransactionsAdded));
            batch.BatchCommands.Add(cmd);
        }

        await batch.PrepareAsync(); // Preparing will speed up the updates, particularly when there are many!

        await batch.ExecuteNonQueryAsync();
        await connection.CloseAsync();
    }

    public async Task InsertAccountStatementEntries(IEnumerable<AccountStatementEntry> entries)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        context.AccountStatementEntries.AddRange(entries);
        await context.SaveChangesAsync();
    }
}