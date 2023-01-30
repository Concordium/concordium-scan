using Application.Api.GraphQL.Accounts;
using HotChocolate.Types.Relay;

namespace Application.Api.GraphQL.Contracts
{
    public class Contract
    {
        [ID]
        public long Id { get; set; }
        public ContractAddress ContractAddress { get; set; }
        public string ModuleRef { get; set; }
        public long Balance { get; set; }
        public long FirstTransactionId { get; set; }
        public long LastTransactionId { get; set; }
        public int TransactionsCount { get; set; }
        public AccountAddress Owner { get; set; }
        public DateTimeOffset CreatedTime { get; set; }
    }
}
