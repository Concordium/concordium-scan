using System.Numerics;
using Application.Api.GraphQL.Accounts;
using Application.Api.GraphQL.EfCore;
using Application.Api.GraphQL.Transactions;
using HotChocolate;
using HotChocolate.Data;
using HotChocolate.Types;
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
        public BigInteger TotalSupply { get; set; }

        /// <summary>
        /// Gets accounts with balances for this particular token
        /// </summary>
        /// <param name="dbContext">EF Core Database Context</param>
        /// <returns><see cref="IQueryable<AccountToken>"/></returns>
        [UseDbContext(typeof(GraphQlDbContext))]
        [UsePaging(InferConnectionNameFromField = false, ProviderName = "token_account_descending")]
        public IQueryable<AccountToken> GetAccounts([ScopedService] GraphQlDbContext dbContext)
        {
            return dbContext.AccountTokens.Where(t =>
                t.ContractIndex == this.ContractIndex
                && t.ContractSubIndex == this.ContractSubIndex
                && t.TokenId == this.TokenId
                && t.Balance != 0)
            .OrderByDescending(t => t.AccountId)
            .AsNoTracking();
        }

        [UseDbContext(typeof(GraphQlDbContext))]
        [UsePaging(InferConnectionNameFromField = false, ProviderName = "token_transaction_descending")]
        public IQueryable<TokenTransaction> GetTransactions([ScopedService] GraphQlDbContext dbContext)
        {
            return dbContext.TokenTransactions.Where(t =>
                t.ContractIndex == this.ContractIndex
                && t.ContractSubIndex == this.ContractSubIndex
                && t.TokenId == this.TokenId)
            .OrderByDescending(t => t.TransactionId)
            .AsNoTracking();
        }

        [UseDbContext(typeof(GraphQlDbContext))]
        public Transaction GetCreateTransaction([ScopedService] GraphQlDbContext dbContext)
        {
            var firstTxnId = dbContext.TokenTransactions
            .Where(t =>
                t.ContractIndex == this.ContractIndex
                && t.ContractSubIndex == this.ContractSubIndex
                && t.TokenId == this.TokenId)
            .Min(t => t.TransactionId);

            return dbContext.Transactions.AsNoTracking().Where(t => t.Id == firstTxnId).Single();
        }
    }
}