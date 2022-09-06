using System.Numerics;

namespace Application.Api.GraphQL.Import.EventLogs
{
    /// <summary>
    /// Represents that a Token was Minted.
    /// </summary>
    public class CisEventTokenAddedUpdate : CisEventTokenUpdate
    {
        /// <summary>
        /// Amount of Token Minted.
        /// </summary>
        public BigInteger AmountDelta { get; set; }
    }
}