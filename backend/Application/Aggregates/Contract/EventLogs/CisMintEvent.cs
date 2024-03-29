using System.IO;
using System.Numerics;
using Application.Aggregates.Contract.Entities;
using Application.Api.GraphQL;

namespace Application.Aggregates.Contract.EventLogs
{
    /// <summary>
    /// Represents a Token Mint Event
    /// <see href="https://proposals.concordium.software/CIS/cis-2.html#mintevent" />
    /// </summary>
    public class CisMintEvent : CisEvent
    {
        public CisMintEvent(
            string tokenId,
            ulong contractIndex,
            ulong contractSubIndex,
            string transactionHash,
            string? parsed,
            BigInteger tokenAmount,
            Address toAddress) : base(contractIndex, contractSubIndex, transactionHash, parsed)
        {
            TokenId = tokenId;
            TokenAmount = tokenAmount;
            ToAddress = toAddress;
        }
        
        /// <summary>
        /// Serialized Token Id of <see cref="CisEvent"/>. Parsed by <see cref="CommonParsers.ParseTokenId(BinaryReader)" />
        /// </summary>
        public string TokenId { get; init;  }

        public BigInteger TokenAmount { get; init; }

        public Address ToAddress { get;  init;}

        public static CisMintEvent Parse(ContractAddress address, BinaryReader st, string transactionHash, string? parsed)
        {
            return new CisMintEvent
            (
                contractIndex: address.Index,
                contractSubIndex:  address.SubIndex,
                tokenId: CommonParsers.ParseTokenId(st),
                tokenAmount: CommonParsers.ParseTokenAmount(st),
                toAddress:  CommonParsers.ParseAddress(st),
                transactionHash: transactionHash,
                parsed: parsed
            );
        }

        internal override TokenEvent GetTokenEvent() => 
            new(ContractIndex, ContractSubIndex, TokenId, this);
    }
}
