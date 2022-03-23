using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
using Application.Api.GraphQL.EfCore;
using Application.Common.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Application.Api.GraphQL.Import;

public class AccountWriter
{
    private readonly IDbContextFactory<GraphQlDbContext> _dbContextFactory;
    private readonly IMetrics _metrics;
    
    public AccountWriter(IDbContextFactory<GraphQlDbContext> dbContextFactory, IMetrics metrics)
    {
        _dbContextFactory = dbContextFactory;
        _metrics = metrics;
    }

    public async Task InsertAccounts(IEnumerable<Account> accounts)
    {
        using var counter = _metrics.MeasureDuration(nameof(AccountWriter), nameof(InsertAccounts));

        await using var context = await _dbContextFactory.CreateDbContextAsync();
        context.Accounts.AddRange(accounts);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAccounts(IEnumerable<AccountUpdate> accountUpdates)
    {
        using var counter = _metrics.MeasureDuration(nameof(AccountWriter), nameof(UpdateAccounts));
        
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

    public async Task InsertAccountTransactionRelation(AccountTransactionRelation[] items)
    {
        if (items.Length == 0) return;

        using var counter = _metrics.MeasureDuration(nameof(AccountWriter), nameof(InsertAccountTransactionRelation));

        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var connection = context.Database.GetDbConnection();

        var sql = @"
                insert into graphql_account_transactions (account_id, transaction_id)
                values (@AccountId, @TransactionId) 
                returning account_id, index, transaction_id;";

        await connection.OpenAsync();

        var batch = connection.CreateBatch();
        foreach (var item in items)
        {
            var cmd = batch.CreateBatchCommand();
            cmd.CommandText = sql;
            cmd.Parameters.Add(new NpgsqlParameter<long>("TransactionId", item.TransactionId));
            cmd.Parameters.Add(new NpgsqlParameter<long>("AccountId", item.AccountId));
            batch.BatchCommands.Add(cmd);
        }

        await batch.PrepareAsync(); // Preparing will speed up the inserts, particularly when there are many!

        await using var reader = await batch.ExecuteReaderAsync();
        var returnedItems = IterateBatchDbDataReader(reader, row => new
            {
                AccountId = row.GetInt64(0),
                Index = row.GetInt64(1),
                TransactionId = row.GetInt64(2)
            })
            .ToArray();

        foreach (var item in items)
        {
            var returnedItem =
                returnedItems.Single(x => x.AccountId == item.AccountId && x.TransactionId == item.TransactionId);
            item.Index = returnedItem.Index;
        }

        await connection.CloseAsync();
    }

    public async Task InsertAccountStatementEntries(IEnumerable<AccountStatementEntry> entries)
    {
        using var counter = _metrics.MeasureDuration(nameof(AccountWriter), nameof(InsertAccountStatementEntries));

        await using var context = await _dbContextFactory.CreateDbContextAsync();
        context.AccountStatementEntries.AddRange(entries);
        await context.SaveChangesAsync();
    }

    public async Task InsertAccountReleaseScheduleItems(IEnumerable<AccountReleaseScheduleItem> items)
    {
        using var counter = _metrics.MeasureDuration(nameof(AccountWriter), nameof(InsertAccountReleaseScheduleItems));

        await using var context = await _dbContextFactory.CreateDbContextAsync();
        context.AddRange(items);
        await context.SaveChangesAsync();
    }

    private static IEnumerable<T> IterateBatchDbDataReader<T>(DbDataReader reader, Func<IDataReader, T> projection)
    {
        do
        {
            while (reader.Read())
            {
                yield return projection(reader);
            }
        } while (reader.NextResult());
    }
}