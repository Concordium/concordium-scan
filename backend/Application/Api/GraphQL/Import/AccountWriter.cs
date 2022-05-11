using System.Data;
using System.Data.Common;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Application.Api.GraphQL.Accounts;
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

    public AccountUpdateResult[] UpdateAccounts(AccountUpdate[] accountUpdates)
    {
        using var counter = _metrics.MeasureDuration(nameof(AccountWriter), nameof(UpdateAccounts));
        
        var sql = @"
            UPDATE graphql_accounts 
            SET ccd_amount = ccd_amount + @AmountAdjustment,
                transaction_count = transaction_count + @TransactionsAdded
            WHERE id = @AccountId
            RETURNING id, ccd_amount";

        using var context = _dbContextFactory.CreateDbContext();
        var connection = context.Database.GetDbConnection();

        connection.Open();

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

        batch.Prepare(); // Preparing will speed up the updates, particularly when there are many!

        using var reader = batch.ExecuteReader();
        var returnedItems = IterateBatchDbDataReader(reader, row => new
            {
                AccountId = row.GetInt64(0),
                CcdAmount = row.GetInt64(1),
            })
            .ToArray();
        
        connection.Close();

        return accountUpdates.Select(update =>
            {
                var updateResult = returnedItems.Single(x => x.AccountId == update.AccountId);
                return new AccountUpdateResult(update.AccountId, (ulong)(updateResult.CcdAmount - update.AmountAdjustment), (ulong)updateResult.CcdAmount);
            })
            .ToArray();
    }

    public void InsertAccountTransactionRelation(AccountTransactionRelation[] items)
    {
        if (items.Length == 0) return;

        using var counter = _metrics.MeasureDuration(nameof(AccountWriter), nameof(InsertAccountTransactionRelation));

        using var context = _dbContextFactory.CreateDbContext();
        var connection = context.Database.GetDbConnection();

        var sql = @"
                insert into graphql_account_transactions (account_id, transaction_id)
                values (@AccountId, @TransactionId) 
                returning account_id, index, transaction_id;";

        connection.Open();

        var batch = connection.CreateBatch();
        foreach (var item in items)
        {
            var cmd = batch.CreateBatchCommand();
            cmd.CommandText = sql;
            cmd.Parameters.Add(new NpgsqlParameter<long>("TransactionId", item.TransactionId));
            cmd.Parameters.Add(new NpgsqlParameter<long>("AccountId", item.AccountId));
            batch.BatchCommands.Add(cmd);
        }

        batch.Prepare(); // Preparing will speed up the inserts, particularly when there are many!

        using var reader = batch.ExecuteReader();
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

        connection.Close();
    }

    public void InsertAccountStatementEntries(IEnumerable<AccountStatementEntry> entries)
    {
        using var counter = _metrics.MeasureDuration(nameof(AccountWriter), nameof(InsertAccountStatementEntries));

        using var context = _dbContextFactory.CreateDbContext();
        context.AccountStatementEntries.AddRange(entries);
        context.SaveChanges();
    }

    public void InsertAccountReleaseScheduleItems(IEnumerable<AccountReleaseScheduleItem> items)
    {
        using var counter = _metrics.MeasureDuration(nameof(AccountWriter), nameof(InsertAccountReleaseScheduleItems));

        using var context = _dbContextFactory.CreateDbContext();
        context.AddRange(items);
        context.SaveChanges();
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

    public async Task UpdateAccount<TSource>(TSource item, Func<TSource, ulong> delegatorIdSelector, Action<TSource, Account> updateAction)
    {
        using var counter = _metrics.MeasureDuration(nameof(AccountWriter), nameof(UpdateAccount));
        
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var delegatorId = (long)delegatorIdSelector(item);

        var account = await context.Accounts.SingleAsync(x => x.Id == delegatorId);
        updateAction(item, account);
        
        await context.SaveChangesAsync();
    }
    
    public async Task UpdateAccountsWithPendingDelegationChange(DateTimeOffset effectiveTimeEqualOrBefore, Action<Account> updateAction)
    {
        using var counter = _metrics.MeasureDuration(nameof(AccountWriter), nameof(UpdateAccountsWithPendingDelegationChange));
        
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        var sql = $"select * from graphql_accounts where delegation_pending_change->'data'->>'EffectiveTime' <= '{effectiveTimeEqualOrBefore:O}'";
        var accounts = await context.Accounts
            .FromSqlRaw(sql)
            .ToArrayAsync();

        foreach (var account in accounts)
            updateAction(account);
            
        await context.SaveChangesAsync();
    }
    
    public async Task UpdateAccounts(Expression<Func<Account, bool>> whereClause, Action<Account> updateAction)
    {
        using var counter = _metrics.MeasureDuration(nameof(AccountWriter), nameof(UpdateAccounts));

        await using var context = await _dbContextFactory.CreateDbContextAsync();

        var accounts = await context.Accounts
            .Where(whereClause)
            .ToArrayAsync();
        
        if (accounts.Length > 0)
        {
            foreach (var account in accounts)
                updateAction(account);
            
            await context.SaveChangesAsync();
        }
    }
    
    public async Task UpdateDelegationStakeIfRestakingEarnings(AccountReward[] stakeUpdates)
    {
        using var counter = _metrics.MeasureDuration(nameof(AccountWriter), nameof(UpdateDelegationStakeIfRestakingEarnings));

        if (stakeUpdates.Length == 0) return;
        
        var sql = @"
            update graphql_accounts 
            set delegation_staked_amount = delegation_staked_amount + @AddedStake 
            where id = @AccountId 
              and delegation_restake_earnings = true";

        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var conn = context.Database.GetDbConnection();

        await conn.OpenAsync();

        var batch = conn.CreateBatch();
        foreach (var stakeUpdate in stakeUpdates)
        {
            var cmd = batch.CreateBatchCommand();
            cmd.CommandText = sql;
            cmd.Parameters.Add(new NpgsqlParameter<long>("AccountId", stakeUpdate.AccountId));
            cmd.Parameters.Add(new NpgsqlParameter<long>("AddedStake", stakeUpdate.RewardAmount));
            batch.BatchCommands.Add(cmd);
        }

        await batch.PrepareAsync(); // Preparing will speed up the updates, particularly when there are many!
        await batch.ExecuteNonQueryAsync();
        
        await conn.CloseAsync();
    }
}

public record AccountUpdate(long AccountId, long AmountAdjustment, int TransactionsAdded);
public record AccountUpdateResult(long AccountId, ulong AccountBalanceBeforeUpdate, ulong AccountBalanceAfterUpdate);
