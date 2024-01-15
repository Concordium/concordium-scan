using System.Threading.Tasks;
using Application.Api.GraphQL;
using Application.Api.GraphQL.Accounts;
using Application.Api.GraphQL.EfCore;
using Application.Api.GraphQL.Transactions;
using HotChocolate;
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
    /// Get transaction with the initial mint event of the token.
    /// </summary>
    public async Task<Transaction> GetInitialTransaction(GraphQlDbContext context)
    {
        var initialTokenEvent = await context.TokenEvents
            .Where(te => te.ContractIndex == ContractIndex &&
                         te.ContractSubIndex == ContractSubIndex &&
                         te.TokenId == TokenId)
            .OrderBy(t => t.Id)
            .FirstAsync();
        
        return await context.Transactions
            .SingleAsync(t => t.TransactionHash == initialTokenEvent.Event.TransactionHash);
    }

    /// <summary>
    /// Gets accounts with balances for this particular token
    /// </summary>
    /// <param name="dbContext">EF Core Database Context</param>
    /// <returns><see cref="IQueryable<AccountToken>"/></returns>
    [UseOffsetPaging(MaxPageSize = 100, IncludeTotalCount = true)]
    public IQueryable<AccountToken> GetAccounts(GraphQlDbContext dbContext)
    {
        return dbContext.AccountTokens
            .AsNoTracking()
            .Where(t =>
            t.ContractIndex == ContractIndex
            && t.ContractSubIndex == ContractSubIndex
            && t.TokenId == TokenId)
            .OrderByDescending(t => t.AccountId);
    }
        
    [UseOffsetPaging(MaxPageSize = 100, IncludeTotalCount = true)]
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

    [ExtendObjectType(typeof(Token))]
    public sealed class TokenExtensions
    {
        public string GetContractName([Parent] Token token) => 
            new ContractAddress(token.ContractIndex, token.ContractSubIndex).AsString;
    }
}

[ExtendObjectType(typeof(Query))]
public class TokenQuery
{
    [UsePaging(MaxPageSize = 100)]
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
