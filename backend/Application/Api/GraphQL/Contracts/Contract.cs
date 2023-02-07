using Application.Api.GraphQL.Accounts;
using HotChocolate;
using HotChocolate.Types.Relay;

namespace Application.Api.GraphQL.Contracts
{
    public class Contract
    {
        [ID]
        /// <summary>
        /// Database Id of this Contract.
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Address if this Contract.
        /// </summary>
        public ContractAddress ContractAddress { get; set; }

        /// <summary>
        /// Last module reference of the contract. 
        /// In case the contract is upgraded this represents the latest module contract executes. 
        /// </summary>
        public string ModuleRef { get; set; }

        /// <summary>
        /// Total Balance of the Contract.
        /// </summary>
        public long Balance { get; set; }

        /// <summary>
        /// First database transaction id which Initialized this Contract.
        /// </summary>
        [GraphQLIgnore]
        public long FirstTransactionId { get; set; }

        /// <summary>
        /// Last database transaction id of the Contract.
        /// </summary>
        [GraphQLIgnore]
        public long LastTransactionId { get; set; }

        /// <summary>
        /// Total no of transactions this Contract has been a part of.
        /// </summary>
        public int TransactionsCount { get; set; }

        /// <summary>
        /// Owner <see cref="AccountAddress"/> of this Contract.
        /// </summary>
        public AccountAddress Owner { get; set; }

        /// <summary>
        /// Block slot time when this Contract was initialized.
        /// </summary>
        public DateTimeOffset CreatedTime { get; set; }
    }
}
