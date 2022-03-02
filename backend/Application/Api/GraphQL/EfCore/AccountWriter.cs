using System.Threading.Tasks;
using ConcordiumSdk.NodeApi.Types;
using Dapper;
using Microsoft.EntityFrameworkCore;

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
            Address = x.AccountAddress.AsString,
            CreatedAt = blockSlotTime
        }).ToArray();
        context.Accounts.AddRange(accounts);
        await context.SaveChangesAsync();
    }

    public async Task AddAccountTransactionRelations(TransactionPair[] transactions)
    {
        var accountTransactions = transactions
            .Select(x => new
            {
                TransactionId = x.Target.Id,
                DistinctAccountAddresses = FindAccountAddresses(x.Source, x.Target).Distinct()
            })
            .SelectMany(x => x.DistinctAccountAddresses
                .Select(accountAddress => new
                {
                    AccountAddress = accountAddress.AsString, 
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
                select id, @TransactionId from graphql_accounts where address = @AccountAddress;", accountTransactions);
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
                        AccountAddress = scheduleEvent.To.AsString,
                        TransactionId = transaction.Target.Id,
                        ScheduleIndex = ix,
                        Timestamp = amount.Timestamp,
                        Amount = Convert.ToInt64(amount.Amount.MicroCcdValue)
                    }));
            }).ToArray();

        if (result.Length > 0)
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync();
            var connection = context.Database.GetDbConnection();

            await connection.ExecuteAsync(@"
                insert into graphql_account_release_schedule (account_id, transaction_id, schedule_index, timestamp, amount)
                select id, @TransactionId, @ScheduleIndex, @Timestamp, @Amount from graphql_accounts where address = @AccountAddress;",
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