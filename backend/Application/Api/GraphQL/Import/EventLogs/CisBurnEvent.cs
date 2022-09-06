using System.IO;
using System.Numerics;

namespace Application.Api.GraphQL.Import.EventLogs
{
    /// <summary>
    /// Represents CIS Burn Event <see href="https://proposals.concordium.software/CIS/cis-2.html#burnevent" />
    /// </summary>
    public class CisBurnEvent : CisEvent
    {
        public CisBurnEvent() : base(CisEventType.Burn)
        {
        }

        /// <summary>
        /// Amount of token burned.
        /// </summary>
        public BigInteger TokenAmount { get; init; }

        /// <summary>
        /// Account/Contract address from which the token was burned.  
        /// </summary>
        /// <value></value>
        public BaseAddress FromAddress { get; init; }

        /// <summary>
        /// Parses the event from bytes.
        /// </summary>
        /// <param name="address">Contract Address emitting the event</param>
        /// <param name="st">Binary Reader</param>
        /// <returns>Parsed <see cref="CisBurnEvent"/></returns>
        public static CisBurnEvent Parse(ConcordiumSdk.Types.ContractAddress address, BinaryReader st)
        {
            return new CisBurnEvent
            {
                ContractIndex = address.Index,
                ContractSubIndex = address.SubIndex,
                // https://proposals.concordium.software/CIS/cis-1.html#tokenid
                TokenId = CommonParsers.ParseTokenId(st),
                TokenAmount = CommonParsers.ParseTokenAmount(st),
                FromAddress = CommonParsers.ParseAddress(st),
            };
        }
    }
}