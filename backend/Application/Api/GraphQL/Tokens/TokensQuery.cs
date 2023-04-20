using Application.Api.GraphQL;
using Application.Api.GraphQL.EfCore;
using Application.Api.GraphQL.Tokens;
using HotChocolate;
using HotChocolate.Data;
using HotChocolate.Types;
using Microsoft.EntityFrameworkCore;

[ExtendObjectType(typeof(Query))]
public class TokensQuery
{
    [UseDbContext(typeof(GraphQlDbContext))]
    [UsePaging]
    public IQueryable<Token> GetTokens([ScopedService] GraphQlDbContext dbContext) => dbContext.Tokens.OrderBy(t => t.ContractIndex).AsNoTracking();
}