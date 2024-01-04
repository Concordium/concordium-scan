using System.IO;
using System.Numerics;
using Application.Api.GraphQL.Tokens;

namespace Application.Api.GraphQL.Import.EventLogs
{
    /// <summary>
    /// Represents a Token Transfer Event
    /// <see href="https://proposals.concordium.software/CIS/cis-2.html#transferevent" />
    /// </summary>
    public class CisTransferEvent : CisEvent
    {
        public CisTransferEvent(
            string tokenId,
            ulong contractIndex,
            ulong contractSubIndex,
            long transactionId,
            BigInteger tokenAmount, 
            Address fromAddress, 
            Address toAddress) : base(contractIndex, contractSubIndex, transactionId)
        {
            TokenId = tokenId;
            TokenAmount = tokenAmount;
            FromAddress = fromAddress;
            ToAddress = toAddress;
        }
        
        /// <summary>
        /// Serialized Token Id of <see cref="CisEvent"/>. Parsed by <see cref="CommonParsers.ParseTokenId(BinaryReader)" />
        /// </summary>
        public string TokenId { get; set; }

        /// <summary>
        /// Amount of token transferred
        /// </summary>
        public BigInteger TokenAmount { get; set; }

        /// <summary>
        /// Transferred from Address
        /// </summary>
        public Address FromAddress { get; set; }

        /// <summary>
        /// Transferred to Address
        /// </summary>
        public Address ToAddress { get; set; }

        public static CisTransferEvent Parse(Concordium.Sdk.Types.ContractAddress address, BinaryReader st, long transactionId)
        {
            return new CisTransferEvent
            (
                contractIndex: address.Index,
                contractSubIndex: address.SubIndex,
                tokenId: CommonParsers.ParseTokenId(st),
                tokenAmount: CommonParsers.ParseTokenAmount(st),
                fromAddress: CommonParsers.ParseAddress(st),
                toAddress: CommonParsers.ParseAddress(st),
                transactionId: transactionId
            );
        }

        internal override TokenEvent? GetTokenEvent() => 
            new(ContractIndex, ContractSubIndex, TokenId, this);
    }
}
