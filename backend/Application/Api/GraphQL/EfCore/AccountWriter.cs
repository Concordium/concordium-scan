using System.Threading.Tasks;
using ConcordiumSdk.NodeApi.Types;
using ConcordiumSdk.Types;
using Dapper;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Application.Api.GraphQL.EfCore;

public class AccountWriter
{
    private readonly IDbContextFactory<GraphQlDbContext> _dbContextFactory;

    public AccountWriter(IDbContextFactory<GraphQlDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task AddAccounts(AccountInfo[] createdAccounts, DateTimeOffset blockSlotTime)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        var accounts = createdAccounts.Select(x => new Account
        {
            Id = (long)x.AccountIndex,
            CanonicalAddress = x.AccountAddress.AsString,
            BaseAddress = new AccountAddress(x.AccountAddress.GetBaseAddress().AsString),
            Amount = x.AccountAmount.MicroCcdValue,
            CreatedAt = blockSlotTime
        }).ToArray();
        context.Accounts.AddRange(accounts);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAccountBalances(BlockSummary blockSummary)
    {
        var balanceUpdates = blockSummary.GetAccountBalanceUpdates();
        
        // TODO: Added "RETURNING base_address, ccd_amount" to be able to write metrics for account balances!
        var sql = @"UPDATE graphql_accounts SET ccd_amount = ccd_amount + @AmountAdjustment WHERE base_address = @BaseAddress";

        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var connection = context.Database.GetDbConnection();
        
        await connection.OpenAsync();
        
        var batch = connection.CreateBatch();
        foreach (var balanceUpdate in balanceUpdates)
        {
            var cmd = batch.CreateBatchCommand();
            cmd.CommandText = sql;
            cmd.Parameters.Add(new NpgsqlParameter<long>("AmountAdjustment", balanceUpdate.AmountAdjustment));
            cmd.Parameters.Add(new NpgsqlParameter<string>("BaseAddress", balanceUpdate.AccountAddress.GetBaseAddress().AsString));
            batch.BatchCommands.Add(cmd);
        }
        await batch.ExecuteNonQueryAsync();
        await connection.CloseAsync();
    }

    public async Task AddAccountTransactionRelations(TransactionPair[] transactions)
    {
        var accountTransactions = transactions
            .Select(x => new
            {
                TransactionId = x.Target.Id,
                DistinctAccountBaseAddresses = FindAccountAddresses(x.Source, x.Target)
                    .Select(address => address.GetBaseAddress())
                    .Distinct()
            })
            .SelectMany(x => x.DistinctAccountBaseAddresses
                .Select(accountBaseAddress => new
                {
                    AccountBaseAddress = accountBaseAddress.AsString, 
                    x.TransactionId
                }))
            .ToArray();

        if (accountTransactions.Length > 0)
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync();
            var connection = context.Database.GetDbConnection();

            // Inserted via dapper to inline lookup of account id from account address directly in insert
            await connection.ExecuteAsync(@"
                insert into graphql_account_transactions (account_id, transaction_id)
                select id, @TransactionId from graphql_accounts where base_address = @AccountBaseAddress;", accountTransactions);
        }
    }
    
    public async Task AddAccountReleaseScheduleItems(IEnumerable<TransactionPair> transactions)
    {
        var result = transactions
            .Where(transaction => transaction.Source.Result is TransactionSuccessResult)
            .SelectMany(transaction =>
            {
                return ((TransactionSuccessResult)transaction.Source.Result).Events
                    .OfType<ConcordiumSdk.NodeApi.Types.TransferredWithSchedule>()
                    .SelectMany(scheduleEvent => scheduleEvent.Amount.Select((amount, ix) => new
                    {
                        AccountBaseAddress = scheduleEvent.To.GetBaseAddress().AsString,
                        TransactionId = transaction.Target.Id,
                        ScheduleIndex = ix,
                        Timestamp = amount.Timestamp,
                        Amount = Convert.ToInt64(amount.Amount.MicroCcdValue),
                        FromAccountBaseAddress = scheduleEvent.From.GetBaseAddress().AsString
                    }));
            }).ToArray();

        if (result.Length > 0)
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync();
            var connection = context.Database.GetDbConnection();

            await connection.ExecuteAsync(@"
                insert into graphql_account_release_schedule (account_id, transaction_id, schedule_index, timestamp, amount, from_account_id)
                values ((select id from graphql_accounts where base_address = @AccountBaseAddress limit 1), @TransactionId, @ScheduleIndex, @Timestamp, @Amount, (select id from graphql_accounts where base_address = @FromAccountBaseAddress limit 1));",
                result);
        }
    }
    private IEnumerable<ConcordiumSdk.Types.AccountAddress> FindAccountAddresses(TransactionSummary source, Transaction mapped)
    {
        if (source.Sender != null) yield return source.Sender;
        foreach (var address in source.Result.GetAccountAddresses())
            yield return address;
    }
}