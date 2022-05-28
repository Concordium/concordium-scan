﻿using Application.Api.GraphQL.EfCore;
using HotChocolate;
using HotChocolate.Data;
using HotChocolate.Types;
using Microsoft.EntityFrameworkCore;

namespace Application.Api.GraphQL.PassiveDelegations;

[ExtendObjectType(typeof(Query))]
public class PassiveDelegationQuery
{
    [UseDbContext(typeof(GraphQlDbContext))]
    public PassiveDelegation? GetPassiveDelegation([ScopedService] GraphQlDbContext dbContext)
    {
        var result = dbContext.PassiveDelegations
            .AsNoTracking()
            .SingleOrDefault();

        return result;
    }
}