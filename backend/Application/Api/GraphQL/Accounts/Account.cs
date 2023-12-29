using System.Threading.Tasks;
using Application.Api.GraphQL.Bakers;
using Application.Api.GraphQL.EfCore;
using HotChocolate;
using HotChocolate.Types;
using HotChocolate.Types.Relay;
using Microsoft.EntityFrameworkCore;

namespace Application.Api.GraphQL.Accounts;

public class Account
{
    [ID]
    public long Id { get; set; }

    [GraphQLIgnore] // Base address is only used internally for handling alias account addresses
    public AccountAddress BaseAddress { get; set; }

    [GraphQLName("address")]
    public AccountAddress CanonicalAddress { get; set; }

    public ulong Amount { get; set; }

    public int TransactionCount { get; set; }

    public DateTimeOffset CreatedAt { get; init; }

    public Delegation? Delegation { get; set; }
    
    public async Task<AccountReleaseSchedule> GetReleaseSchedule(GraphQlDbContext dbContext)
    {
        var schedule = await dbContext.AccountReleaseScheduleItems.AsNoTracking()
            .Where(x => x.AccountId == Id && x.Timestamp > DateTimeOffset.UtcNow)
            .OrderBy(x => x.Timestamp)
            .ToArrayAsync();

        return new AccountReleaseSchedule(schedule);
    }
    
    [UsePaging(InferConnectionNameFromField = false, ProviderName = "account_transaction_relation_by_descending_index")]
    public IQueryable<AccountTransactionRelation> GetTransactions(GraphQlDbContext dbContext)
    {
        return dbContext.AccountTransactionRelations
            .AsNoTracking()
            .Where(at => at.AccountId == Id)
            .OrderByDescending(x => x.Index);
    }
    
    [UsePaging(InferConnectionNameFromField = false, ProviderName = "account_statement_entry_by_descending_index")]
    // TODO: Add a filter on entry type
    public IQueryable<AccountStatementEntry> GetAccountStatement(GraphQlDbContext dbContext)
    {
        return dbContext.AccountStatementEntries.AsNoTracking()
            .Where(x => x.AccountId == Id)
            .OrderByDescending(x => x.Index);
    }
    
    [UsePaging(InferConnectionNameFromField = false, ProviderName = "account_reward_by_descending_index")]
    public IQueryable<AccountReward> GetRewards(GraphQlDbContext dbContext)
    {
        return dbContext.AccountRewards.AsNoTracking()
            .Where(x => x.AccountId == Id)
            .OrderByDescending(x => x.Index);
    }
    
    public Task<Baker?> GetBaker(GraphQlDbContext dbContext)
    {
        // Account and baker share the same ID!
        return dbContext.Bakers
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == Id);
    }

    /// <summary>
    /// Gets CIS tokens assigned to the current account.
    /// </summary>
    /// <param name="dbContext">Database Context</param>
    /// <returns></returns>
    [UsePaging(InferConnectionNameFromField = false, ProviderName = "account_token_descending")]
    public IQueryable<AccountToken> GetTokens(GraphQlDbContext dbContext)
    {
        return dbContext.AccountTokens
            .Where(t => t.AccountId == this.Id && t.Balance != 0)
            .OrderByDescending(t => t.Index)
            .Include(t=>t.Token)
            .AsNoTracking();
    }
}
