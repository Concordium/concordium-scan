using System.IO;
using System.Numerics;
using Application.Api.GraphQL.Tokens;

namespace Application.Api.GraphQL.Import.EventLogs
{
    /// <summary>
    /// Represents CIS Burn Event <see href="https://proposals.concordium.software/CIS/cis-2.html#burnevent" />
    /// </summary>
    public class CisBurnEvent : CisEvent
    {
        public CisBurnEvent(
            Address fromAddress,
            BigInteger tokenAmount,
            string tokenId,
            ulong contractIndex,
            ulong contractSubIndex,
            long transactionId) : base(contractIndex, contractSubIndex, transactionId)
        {
            FromAddress = fromAddress;
            TokenAmount = tokenAmount;
            TokenId = tokenId;
        }
        
        /// <summary>
        /// Serialized Token Id of <see cref="CisEvent"/>. Parsed by <see cref="CommonParsers.ParseTokenId(BinaryReader)" />
        /// </summary>
        public string TokenId { get; set; }

        /// <summary>
        /// Amount of token burned.
        /// </summary>
        public BigInteger TokenAmount { get; set; }

        /// <summary>
        /// Account/Contract address from which the token was burned.  
        /// </summary>
        /// <value></value>
        public Address FromAddress { get; set; }

        /// <summary>
        /// Parses the event from bytes.
        /// </summary>
        /// <param name="address">Contract Address emitting the event</param>
        /// <param name="st">Binary Reader</param>
        /// <param name="transactionId">Transaction Id</param>
        /// <returns>Parsed <see cref="CisBurnEvent"/></returns>
        public static CisBurnEvent Parse(Concordium.Sdk.Types.ContractAddress address, BinaryReader st, long transactionId)
        {
            return new CisBurnEvent
            (
                contractIndex: address.Index,
                contractSubIndex: address.SubIndex,
                // https://proposals.concordium.software/CIS/cis-1.html#tokenid
                tokenId: CommonParsers.ParseTokenId(st),
                tokenAmount: CommonParsers.ParseTokenAmount(st),
                fromAddress: CommonParsers.ParseAddress(st),
                transactionId: transactionId
            );
        }

        internal override TokenEvent GetTokenEvent() => 
            new(ContractIndex, ContractSubIndex, TokenId, this);
    }
}
