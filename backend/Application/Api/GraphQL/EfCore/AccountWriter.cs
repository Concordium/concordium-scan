using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
using ConcordiumSdk.NodeApi.Types;
using Dapper;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Application.Api.GraphQL.EfCore;

public class AccountWriter
{
    private readonly IDbContextFactory<GraphQlDbContext> _dbContextFactory;
    private readonly IAccountLookup _accountLookup;

    public AccountWriter(IDbContextFactory<GraphQlDbContext> dbContextFactory, IAccountLookup accountLookup)
    {
        _dbContextFactory = dbContextFactory;
        _accountLookup = accountLookup;
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

        foreach (var account in accounts)
            _accountLookup.AddToCache(account.BaseAddress.AsString, account.Id);
    }

    public async Task UpdateAccountBalances(BlockSummary blockSummary, AccountTransactionRelation[] transactionRelations)
    {
        var balanceUpdates = blockSummary.GetAccountBalanceUpdates();

        var accountUpdates = await GetAggregatedAccountUpdatesAsync(balanceUpdates, transactionRelations);
        
        await ExecuteAccountUpdates(accountUpdates);
    }

    private async Task ExecuteAccountUpdates(IEnumerable<AccountUpdate> accountUpdates)
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

    public async Task<IEnumerable<AccountUpdate>> GetAggregatedAccountUpdatesAsync(IEnumerable<AccountBalanceUpdate> balanceUpdates, AccountTransactionRelation[] transactionRelations)
    {
        var aggregatedBalanceUpdates = balanceUpdates
            .Select(x => new { BaseAddress = x.AccountAddress.GetBaseAddress().AsString, x.AmountAdjustment })
            .GroupBy(x => x.BaseAddress)
            .Select(addressGroup => new
            {
                BaseAddress = addressGroup.Key,
                AmountAdjustment = addressGroup.Aggregate(0L, (acc, item) => acc + item.AmountAdjustment)
            })
            .ToArray();

        var baseAddresses = aggregatedBalanceUpdates.Select(x => x.BaseAddress);
        var accountIdLookup = await _accountLookup.GetAccountIdsFromBaseAddressesAsync(baseAddresses);
        
        var amountAdjustmentResults = aggregatedBalanceUpdates
            .Select(x =>
            {
                var accountId = accountIdLookup[x.BaseAddress] ?? throw new InvalidOperationException("Attempt at updating account that does not exist!");
                return new AccountUpdate(accountId, x.AmountAdjustment, 0);
            })
            .Where(x => x.AmountAdjustment != 0);

        var transactionResults = transactionRelations
            .GroupBy(x => x.AccountId)
            .Select(x => new { AccountId = x.Key, TransactionsAdded = x.Aggregate(0, (acc, _) => ++acc) })
            .Select(x => new AccountUpdate(x.AccountId, 0, x.TransactionsAdded));

        return amountAdjustmentResults.Concat(transactionResults)
            .GroupBy(x => x.AccountId)
            .Select(group => new AccountUpdate(group.Key,
                group.Aggregate(0L, (acc, item) => acc + item.AmountAdjustment),
                group.Aggregate(0, (acc, item) => acc + item.TransactionsAdded)));
    }
    
    public record AccountUpdate(long AccountId, long AmountAdjustment, int TransactionsAdded);

    public async Task<AccountTransactionRelation[]> AddAccountTransactionRelations(TransactionPair[] transactions)
    {
        var accountTransactions = transactions
            .Select(x => new
            {
                TransactionId = x.Target.Id,
                DistinctAccountBaseAddresses = FindAccountAddresses(x.Source)
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
            var distinctBaseAddresses = accountTransactions
                .Select(x => x.AccountBaseAddress)
                .Distinct();
            var accountIdLookup = await _accountLookup.GetAccountIdsFromBaseAddressesAsync(distinctBaseAddresses);
            
            await using var context = await _dbContextFactory.CreateDbContextAsync();
            var connection = context.Database.GetDbConnection();

            var sql = @"
                insert into graphql_account_transactions (account_id, transaction_id)
                values (@AccountId, @TransactionId) 
                returning account_id, index, transaction_id;";

            await connection.OpenAsync();
        
            var batch = connection.CreateBatch();
            foreach (var accountTransaction in accountTransactions)
            {
                var accountId = accountIdLookup[accountTransaction.AccountBaseAddress];
                if (accountId.HasValue)
                {
                    var cmd = batch.CreateBatchCommand();
                    cmd.CommandText = sql;
                    cmd.Parameters.Add(new NpgsqlParameter<long>("TransactionId", accountTransaction.TransactionId));
                    cmd.Parameters.Add(new NpgsqlParameter<long>("AccountId", accountId.Value));
                    batch.BatchCommands.Add(cmd);
                }
            }

            await batch.PrepareAsync(); // Preparing will speed up the inserts, particularly when there are many!
            
            await using var reader = await batch.ExecuteReaderAsync();
            var result = IterateBatchDbDataReader(reader, row => new AccountTransactionRelation()
                {
                    AccountId = row.GetInt64(0),
                    Index = row.GetInt64(1),
                    TransactionId = row.GetInt64(2)
                })
                .ToArray();

            await connection.CloseAsync();
            return result;
        }

        return Array.Empty<AccountTransactionRelation>();
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
    private IEnumerable<ConcordiumSdk.Types.AccountAddress> FindAccountAddresses(TransactionSummary source)
    {
        if (source.Sender != null) yield return source.Sender;
        foreach (var address in source.Result.GetAccountAddresses())
            yield return address;
    }
}
