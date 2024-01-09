using Application.Api.GraphQL;
using Application.Api.GraphQL.Accounts;
using Application.Api.GraphQL.EfCore;
using HotChocolate.Types;
using Microsoft.EntityFrameworkCore;

namespace Application.Aggregates.Contract.Entities;

/// <summary>
/// Represents CIS token in database
/// </summary>
public class Token
{
    /// <summary>
    /// Token Contract Index
    /// </summary>
    public ulong ContractIndex { get; set; }

    /// <summary>
    /// Token contract Subindex
    /// </summary>
    public ulong ContractSubIndex { get; set; }

    /// <summary>
    /// Token Id
    /// </summary>
    public string TokenId { get; set; }

    /// <summary>
    /// Token Metadata URL
    /// </summary>
    public string? MetadataUrl { get; set; }

    /// <summary>
    /// Total supply of the token
    /// </summary>
    public decimal TotalSupply { get; set; }

    /// <summary>
    /// Gets accounts with balances for this particular token
    /// </summary>
    /// <param name="dbContext">EF Core Database Context</param>
    /// <returns><see cref="IQueryable<AccountToken>"/></returns>
    public IQueryable<AccountToken> GetTokens(GraphQlDbContext dbContext)
    {
        return dbContext.AccountTokens.AsNoTracking().Where(t =>
            t.ContractIndex == ContractIndex
            && t.ContractSubIndex == ContractSubIndex
            && t.TokenId == TokenId);
    }
        
    public IQueryable<TokenEvent> GetTokenEvents(GraphQlDbContext dbContext)
    {
        return dbContext.TokenEvents
            .AsNoTracking()
            .Where(t =>
                t.ContractIndex == this.ContractIndex
                && t.ContractSubIndex == this.ContractSubIndex
                && t.TokenId == this.TokenId)
            .OrderByDescending(t => t.Id);
    }
}

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