using Application.Api.GraphQL.EfCore;
using Application.Api.GraphQL.Transactions;
using HotChocolate;
using HotChocolate.Data;
using HotChocolate.Types.Relay;
using Microsoft.EntityFrameworkCore;

namespace Application.Api.GraphQL.Contracts
{
    public class ContractTransactionRelation
    {
        /// <summary>
        /// Not part of schema. Only here to be able to query relations for specific accounts. 
        /// </summary>
        [GraphQLIgnore]
        public ContractAddress ContractAddress { get; set; }

        /// <summary>
        /// Not part of schema. Only here to be able to retrieve the transaction. 
        /// </summary>
        [GraphQLIgnore]
        [ID]
        public long TransactionId { get; set; }

        [UseDbContext(typeof(GraphQlDbContext))]
        public Transaction GetTransaction([ScopedService] GraphQlDbContext dbContext)
        {
            return dbContext.Transactions
                .AsNoTracking()
                .Single(tx => tx.Id == TransactionId);
        }
    }
}
