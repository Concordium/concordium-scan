using System.Numerics;
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
    public IQueryable<Token> GetTokens([ScopedService] GraphQlDbContext dbContext) => dbContext.Tokens.OrderByDescending(t => t.ContractIndex).AsNoTracking();
    
    [UseDbContext(typeof(GraphQlDbContext))]
    public Token GetToken(
        [ScopedService] GraphQlDbContext dbContext,
        ulong contractIndex,
        ulong contractSubIndex,
        string tokenId) => dbContext.Tokens
            .AsNoTracking()
            .Single(t => t.ContractIndex == contractIndex && t.ContractSubIndex == contractSubIndex && t.TokenId == tokenId);
}