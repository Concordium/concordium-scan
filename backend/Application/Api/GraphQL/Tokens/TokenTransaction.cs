using Application.Api.GraphQL.EfCore;
using Application.Api.GraphQL.Transactions;
using HotChocolate;
using HotChocolate.Data;
using HotChocolate.Types.Relay;
using Microsoft.EntityFrameworkCore;

namespace Application.Api.GraphQL.Tokens
{
    public class TokenTransaction
    {
        [ID]
        public long Id { get; set; }
        public ulong ContractIndex { get; set; }
        public ulong ContractSubIndex { get; set; }
        public string TokenId { get; set; }
        public long TransactionId { get; set; }
        public CisEventData Data { get; set; }

        public TokenTransaction(
            ulong contractIndex,
            ulong contractSubIndex,
            string tokenId,
            long transactionId,
            CisEventData data)
        {
            ContractIndex = contractIndex;
            ContractSubIndex = contractSubIndex;
            TokenId = tokenId;
            TransactionId = transactionId;
            Data = data;
        }

        [UseDbContext(typeof(GraphQlDbContext))]
        public Transaction? GetTransaction([ScopedService] GraphQlDbContext dbContext)
        {
            return dbContext.Transactions.AsNoTracking().Where(t => t.Id == this.TransactionId).SingleOrDefault();
        }
    }
}