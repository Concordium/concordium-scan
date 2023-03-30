using Application.Api.GraphQL.Tokens;
using HotChocolate;
using HotChocolate.Types.Relay;

namespace Application.Api.GraphQL.Accounts
{
    /// <summary>
    /// Represents Mapping of Account and CIS Token in Database.
    /// </summary>
    public class AccountToken
    {

        /// <summary>
        /// Serially increasing Index for Account Token.
        /// </summary>
        public long Index { get; set; }

        /// <summary>
        /// CIS token Contract Index
        /// </summary>
        public ulong ContractIndex { get; set; }

        /// <summary>
        /// CIS token contract subindex
        /// </summary>
        public ulong ContractSubIndex { get; set; }

        /// <summary>
        /// CIS token Id
        /// </summary>
        public string TokenId { get; set; }

        /// <summary>
        /// Token balance for this Account
        /// </summary>
        public double Balance { get; set; }

        /// <summary>
        /// Related <see cref="Token"/>
        /// </summary>
        public Token Token { get; set; }

        /// <summary>
        /// Id of the Account in <see cref="Account"/>
        /// </summary>
        public long AccountId { get; set; }
    }
}
