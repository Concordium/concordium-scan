using System.Threading.Tasks;
using Application.Api.GraphQL.EfCore;
using HotChocolate;
using HotChocolate.Data;
using HotChocolate.Types;
using HotChocolate.Types.Relay;
using Microsoft.EntityFrameworkCore;

namespace Application.Api.GraphQL;

public class Account
{
    [ID]
    public long Id { get; set; }

    [GraphQLIgnore] // Base address is only used internally for handling alias account addresses
    public AccountAddress BaseAddress { get; set; }
    
    [GraphQLName("address")]
    [GraphQLDeprecated("Use 'addressString' instead. Type of this field will be changed to AccountAddress in the near future.")]
    public string CanonicalAddress { get; set; }
    
    public string AddressString => CanonicalAddress;
    
    public ulong Amount { get; set; }
    
    [GraphQLIgnore] // TODO: Add to schema when data import has been validated
    public int TransactionCount { get; set; }
    
    public DateTimeOffset CreatedAt { get; init; }

    [UseDbContext(typeof(GraphQlDbContext))]
    public async Task<AccountReleaseSchedule> GetReleaseSchedule([ScopedService] GraphQlDbContext dbContext)
    {
        var schedule = await dbContext.AccountReleaseScheduleItems.AsNoTracking()
            .Where(x => x.AccountId == Id && x.Timestamp > DateTimeOffset.UtcNow)
            .OrderBy(x => x.Timestamp)
            .ToArrayAsync();
        
        return new AccountReleaseSchedule(schedule);
    }
    
    [UseDbContext(typeof(GraphQlDbContext))]
    [UsePaging(InferConnectionNameFromField = false, ProviderName = "account_transaction_relation_by_descending_index")]
    public IQueryable<AccountTransactionRelation> GetTransactions([ScopedService] GraphQlDbContext dbContext)
    {
        return dbContext.AccountTransactionRelations
            .AsNoTracking()
            .Where(at => at.AccountId == Id)
            .OrderByDescending(x => x.Index);
    }
}

public class AccountReleaseSchedule
{
    [UsePaging(InferConnectionNameFromField = false)]
    public AccountReleaseScheduleItem[] Schedule { get; }

    public ulong TotalAmount { get; }

    public AccountReleaseSchedule(AccountReleaseScheduleItem[] schedule)
    {
        Schedule = schedule;
        TotalAmount = schedule.Aggregate(0UL, (val, item) => val + item.Amount);
    }
}

public class AccountReleaseScheduleItem
{
    /// <summary>
    /// Not part of schema. Only here to be able to query relations for specific accounts. 
    /// </summary>
    [GraphQLIgnore]
    public long AccountId { get; set; }

    /// <summary>
    /// Not part of schema. Only here to be able to query transaction. 
    /// </summary>
    [GraphQLIgnore]
    public long TransactionId { get; set; }

    /// <summary>
    /// Not part of schema. Index is mostly there to ensure primary key is unique in very theoretical situations. 
    /// </summary>
    [GraphQLIgnore]
    public long Index { get; set; }
    
    public DateTimeOffset Timestamp { get; set; }
    
    public ulong Amount { get; set; }
    
    /// <summary>
    /// Not part of schema. Field used for some internal querying of amounts locked in release schedules. 
    /// </summary>
    [GraphQLIgnore]
    public long FromAccountId { get; set; }

    [UseDbContext(typeof(GraphQlDbContext))]
    public async Task<Transaction> GetTransaction([ScopedService] GraphQlDbContext dbContext)
    {
        return await dbContext.Transactions.AsNoTracking()
            .SingleAsync(x => x.Id == TransactionId);
    }
}