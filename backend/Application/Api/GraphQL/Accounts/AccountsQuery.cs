using Application.Api.GraphQL.EfCore;
using HotChocolate;
using HotChocolate.Data;
using HotChocolate.Types;
using HotChocolate.Types.Relay;
using Microsoft.EntityFrameworkCore;

namespace Application.Api.GraphQL.Accounts;

[ExtendObjectType(typeof(Query))]
public class AccountsQuery
{
    [UseDbContext(typeof(GraphQlDbContext))]
    public Account? GetAccount([ScopedService] GraphQlDbContext dbContext, [ID] long id)
    {
        return dbContext.Accounts
            .AsNoTracking()
            .SingleOrDefault(account => account.Id == id);
    }
    
    [UseDbContext(typeof(GraphQlDbContext))]
    public Account? GetAccountByAddress([ScopedService] GraphQlDbContext dbContext, string accountAddress)
    {
        return dbContext.Accounts
            .AsNoTracking()
            .SingleOrDefault(account => account.Address == accountAddress);
    }
    
    [UseDbContext(typeof(GraphQlDbContext))]
    [UsePaging(ProviderName = "account_by_descending_id")]
    public IQueryable<Account> GetAccounts([ScopedService] GraphQlDbContext dbContext)
    {
        return dbContext.Accounts
            .AsNoTracking()
            .OrderByDescending(a => a.Id);
    }
}