using Application.Api.GraphQL.Accounts;
using Application.Api.GraphQL.EfCore;
using HotChocolate;
using HotChocolate.Data;
using Microsoft.EntityFrameworkCore;

namespace Application.Api.GraphQL.Tokens
{
    public class Token
    {
        public ulong ContractIndex { get; set; }

        public ulong ContractSubIndex { get; set; }

        /// <summary>
        /// Token Id
        /// </summary>
        public string TokenId { get; set; }

        public string? MetadataUrl { get; set; }

        public decimal TotalSupply { get; set; }

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