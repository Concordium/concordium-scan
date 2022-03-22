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
        if (!ConcordiumSdk.Types.AccountAddress.TryParse(accountAddress, out var parsed)) 
            return null;
        
        var baseAddress = new AccountAddress(parsed!.GetBaseAddress().AsString);
        return dbContext.Accounts
            .AsNoTracking()
            .SingleOrDefault(account => account.BaseAddress == baseAddress);
    }
    
    [UseDbContext(typeof(GraphQlDbContext))]
    [UsePaging]  // NOTE: Sorting will cause pages to be unstable (if account is update between page loads it might have shifted between pages)  
    public IQueryable<Account> GetAccounts([ScopedService] GraphQlDbContext dbContext, AccountSort sort = AccountSort.AgeDesc)
    {
        var result = dbContext.Accounts
            .AsNoTracking();

        return sort switch
        {
            AccountSort.AgeAsc => result.OrderBy(x => x.Id),
            AccountSort.AgeDesc => result.OrderByDescending(x => x.Id),
            AccountSort.AmountAsc => result.OrderBy(x => x.Amount),
            AccountSort.AmountDesc => result.OrderByDescending(x => x.Amount),
            AccountSort.TransactionCountAsc => result.OrderBy(x => x.TransactionCount),
            AccountSort.TransactionCountDesc => result.OrderByDescending(x => x.TransactionCount),
            _ => throw new NotImplementedException()
        };
    }
}

public enum AccountSort
{
    AgeAsc,
    AgeDesc,
    AmountAsc,
    AmountDesc,
    TransactionCountAsc,
    TransactionCountDesc
}