using Application.Api.GraphQL.Accounts;
using Application.Api.GraphQL.Bakers;
using Application.Api.GraphQL.EfCore;
using HotChocolate;
using HotChocolate.Data;
using HotChocolate.Types;
using Microsoft.EntityFrameworkCore;

namespace Application.Api.GraphQL.PassiveDelegations;

public record PassiveDelegation(
    CommissionRates CommissionRates)
{
    [UseDbContext(typeof(GraphQlDbContext))]
    [UsePaging(DefaultPageSize = 10)]
    public IQueryable<DelegationSummary> GetDelegators([ScopedService] GraphQlDbContext dbContext)
    {
        return dbContext.Accounts.AsNoTracking()
            .Where(x => x.Delegation!.DelegationTarget == new PassiveDelegationTarget())
            .OrderByDescending(x => x.Delegation!.StakedAmount)
            .Select(x => new DelegationSummary(x.CanonicalAddress, x.Delegation!.StakedAmount, x.Delegation.RestakeEarnings));
    }
}