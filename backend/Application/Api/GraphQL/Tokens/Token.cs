using Application.Api.GraphQL.Accounts;
using Application.Api.GraphQL.EfCore;
using HotChocolate;
using HotChocolate.Data;
using Microsoft.EntityFrameworkCore;

namespace Application.Api.GraphQL.Tokens
{
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
        [UseDbContext(typeof(GraphQlDbContext))]
        public IQueryable<AccountToken> GetTokens([ScopedService] GraphQlDbContext dbContext)
        {
            return dbContext.AccountTokens.AsNoTracking().Where(t =>
                t.ContractIndex == this.ContractIndex
                && t.ContractSubIndex == this.ContractSubIndex
                && t.TokenId == this.TokenId);
        }
    }
}