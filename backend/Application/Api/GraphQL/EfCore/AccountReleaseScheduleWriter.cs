using System.Threading.Tasks;
using Application.Database;
using ConcordiumSdk.NodeApi.Types;
using Dapper;
using Npgsql;

namespace Application.Api.GraphQL.EfCore;

public class AccountReleaseScheduleWriter
{
    private readonly DatabaseSettings _dbSettings;

    public AccountReleaseScheduleWriter(DatabaseSettings dbSettings)
    {
        _dbSettings = dbSettings;
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
            await using var connection = new NpgsqlConnection(_dbSettings.ConnectionString);
            await connection.OpenAsync();

            await connection.ExecuteAsync(@"
                insert into graphql_account_release_schedule (account_id, transaction_id, schedule_index, timestamp, amount)
                select id, @TransactionId, @ScheduleIndex, @Timestamp, @Amount from graphql_accounts where address = @AccountAddress;",
                result);
        }
    }
}