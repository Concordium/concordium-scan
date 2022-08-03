using Application.Api.GraphQL.Tokens;
using HotChocolate;
using HotChocolate.Types.Relay;

namespace Application.Api.GraphQL.Accounts
{
    public class AccountToken
    {
        public ulong ContractIndex { get; set; }
        public ulong ContractSubIndex { get; set; }
        public string TokenId { get; set; }
        public long Balance { get; set; }
        public Token Token { get; set; }
        public long AccountId { get; set; }
    }
}