using System.Numerics;
using ConcordiumSdk.Types;

namespace Application.Api.GraphQL.Import.EventLogs
{
    /// <summary>
    /// Computed account update for a CIS token.
    /// </summary>
    public class CisAccountUpdate
    {
        /// <summary>
        /// Contract Index of the Token's Contract
        /// </summary>
        public ulong ContractIndex { get; set; }

        /// <summary>
        /// Contract subindex of the Token's Contract
        /// </summary>
        public ulong ContractSubIndex { get; set; }

        /// <summary>
        /// Token id
        /// </summary>
        public string TokenId { get; set; }

        /// <summary>
        /// Change in amount of the token from the Address. This can be both positive and negative.
        /// </summary>
        /// <value></value>
        public BigInteger AmountDelta { get; set; }

        /// <summary>
        /// Account Address to which this change is to be applied in database.
        /// </summary>
        /// <value></value>
        public AccountAddress Address { get; set; }
    }
}