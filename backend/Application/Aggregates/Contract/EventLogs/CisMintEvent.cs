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
            long transactionId,
            BigInteger tokenAmount,
            Address toAddress) : base(contractIndex, contractSubIndex, transactionId)
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

        public static CisMintEvent Parse(Concordium.Sdk.Types.ContractAddress address, BinaryReader st, long transactionId)
        {
            return new CisMintEvent
            (
                contractIndex: address.Index,
                contractSubIndex:  address.SubIndex,
                tokenId: CommonParsers.ParseTokenId(st),
                tokenAmount: CommonParsers.ParseTokenAmount(st),
                toAddress:  CommonParsers.ParseAddress(st),
                transactionId: transactionId
            );
        }

        internal override TokenEvent GetTokenEvent() => 
            new(ContractIndex, ContractSubIndex, TokenId, this);
    }
}
