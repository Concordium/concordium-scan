﻿using Application.Api.GraphQL.EfCore;
using HotChocolate.Types;
using HotChocolate.Types.Relay;
using Microsoft.EntityFrameworkCore;

namespace Application.Api.GraphQL.Accounts;

[ExtendObjectType(typeof(Query))]
public class AccountsQuery
{
    public Account? GetAccount(GraphQlDbContext dbContext, [ID] long id)
    {
        return dbContext.Accounts
            .AsNoTracking()
            .SingleOrDefault(account => account.Id == id);
    }
    
    public Account? GetAccountByAddress(GraphQlDbContext dbContext, string accountAddress)
    {
        if (!Concordium.Sdk.Types.AccountAddress.TryParse(accountAddress, out var parsed)) 
            return null;
        
        var baseAddress = AccountAddress.From(parsed!.GetBaseAddress());
        return dbContext.Accounts
            .AsNoTracking()
            .SingleOrDefault(account => account.BaseAddress == baseAddress);
    }
    
    [UsePaging]  // NOTE: Sorting will cause pages to be unstable (if account is updated between page loads it might have shifted between pages)  
    public IQueryable<Account> GetAccounts(GraphQlDbContext dbContext, AccountSort sort = AccountSort.AgeDesc, AccountFilter? filter = null)
    {
        var result = dbContext.Accounts
            .AsNoTracking();

        if (filter is { IsDelegator: true })
            result = result.Where(x => x.Delegation.StakedAmount != null);
        else if (filter is { IsDelegator: false })
            result = result.Where(x => x.Delegation.StakedAmount == null);
        
        result = sort switch
        {
            AccountSort.AgeAsc => result.OrderBy(x => x.Id),
            AccountSort.AgeDesc => result.OrderByDescending(x => x.Id),
            AccountSort.AmountAsc => result.OrderBy(x => x.Amount),
            AccountSort.AmountDesc => result.OrderByDescending(x => x.Amount),
            AccountSort.TransactionCountAsc => result.OrderBy(x => x.TransactionCount),
            AccountSort.TransactionCountDesc => result.OrderByDescending(x => x.TransactionCount),
            AccountSort.DelegatedStakeAsc => result.OrderBy(x => x.Delegation.StakedAmount != null).ThenBy(x => x.Delegation.StakedAmount),
            AccountSort.DelegatedStakeDesc => result.OrderBy(x => x.Delegation.StakedAmount == null).ThenByDescending(x => x.Delegation.StakedAmount),
            _ => throw new NotImplementedException()
        };

        return result;
    }
}

public enum AccountSort
{
    AgeAsc,
    AgeDesc,
    AmountAsc,
    AmountDesc,
    TransactionCountAsc,
    TransactionCountDesc,
    DelegatedStakeAsc,
    DelegatedStakeDesc
}

public class AccountFilter
{
    public bool? IsDelegator { get; set; }
}
