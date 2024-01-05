using System.IO;
using System.Numerics;
using Application.Aggregates.Contract.Entities;
using Application.Api.GraphQL;

namespace Application.Aggregates.Contract.EventLogs
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
        public string TokenId { get; init; }

        /// <summary>
        /// Amount of token transferred
        /// </summary>
        public BigInteger TokenAmount { get; init; }

        /// <summary>
        /// Transferred from Address
        /// </summary>
        public Address FromAddress { get; init; }

        /// <summary>
        /// Transferred to Address
        /// </summary>
        public Address ToAddress { get; init; }

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
