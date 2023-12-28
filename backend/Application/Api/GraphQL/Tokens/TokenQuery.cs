using Application.Api.GraphQL.EfCore;
using HotChocolate.Types;
using Microsoft.EntityFrameworkCore;

namespace Application.Api.GraphQL.Tokens;

[ExtendObjectType(typeof(Query))]
public class TokenQuery
{
    [UsePaging]
    public IQueryable<Token> GetTokens(GraphQlDbContext dbContext) =>
        dbContext.Tokens.OrderByDescending(t => t.ContractIndex).AsNoTracking();

    public Token GetToken(
        GraphQlDbContext dbContext,
        ulong contractIndex,
        ulong contractSubIndex,
        string tokenId) => dbContext.Tokens
        .AsNoTracking()
        .Single(t =>
            t.ContractIndex == contractIndex && t.ContractSubIndex == contractSubIndex && t.TokenId == tokenId);
}
