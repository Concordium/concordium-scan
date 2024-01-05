using System.IO;
using System.Numerics;
using Application.Aggregates.Contract.Entities;
using Application.Api.GraphQL;

namespace Application.Aggregates.Contract.EventLogs
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
            string transactionHash,
            string? parsed) : base(contractIndex, contractSubIndex, transactionHash, parsed)
        {
            FromAddress = fromAddress;
            TokenAmount = tokenAmount;
            TokenId = tokenId;
        }
        
        /// <summary>
        /// Serialized Token Id of <see cref="CisEvent"/>. Parsed by <see cref="CommonParsers.ParseTokenId(BinaryReader)" />
        /// </summary>
        public string TokenId { get; init; }

        /// <summary>
        /// Amount of token burned.
        /// </summary>
        public BigInteger TokenAmount { get; init; }

        /// <summary>
        /// Account/Contract address from which the token was burned.  
        /// </summary>
        /// <value></value>
        public Address FromAddress { get; init; }

        /// <summary>
        /// Parses the event from bytes.
        /// </summary>
        /// <param name="address">Contract Address emitting the event</param>
        /// <param name="st">Binary Reader</param>
        /// <param name="transactionHash">Transaction Hash</param>
        /// <param name="parsed">Parsed event in human interpretable form.</param>
        /// <returns>Parsed <see cref="CisBurnEvent"/></returns>
        public static CisBurnEvent Parse(ContractAddress address, BinaryReader st, string transactionHash, string? parsed)
        {
            return new CisBurnEvent
            (
                contractIndex: address.Index,
                contractSubIndex: address.SubIndex,
                // https://proposals.concordium.software/CIS/cis-1.html#tokenid
                tokenId: CommonParsers.ParseTokenId(st),
                tokenAmount: CommonParsers.ParseTokenAmount(st),
                fromAddress: CommonParsers.ParseAddress(st),
                transactionHash: transactionHash,
                parsed: parsed
            );
        }

        internal override TokenEvent GetTokenEvent() => 
            new(ContractIndex, ContractSubIndex, TokenId, this);
    }
}
