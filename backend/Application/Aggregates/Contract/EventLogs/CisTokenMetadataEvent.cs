using System.IO;
using Application.Aggregates.Contract.Entities;
using Application.Api.GraphQL;

namespace Application.Aggregates.Contract.EventLogs
{
    /// <summary>
    /// Represents a Token Metadata Event
    /// <see href="https://proposals.concordium.software/CIS/cis-2.html#tokenmetadataevent" />
    /// </summary>
    public class CisTokenMetadataEvent : CisEvent
    {
        public CisTokenMetadataEvent(
            string tokenId,
            ulong contractIndex,
            ulong contractSubIndex,
            string transactionHash, 
            string? parsed,
            string metadataUrl, 
            string? hashHex) : base(contractIndex, contractSubIndex, transactionHash, parsed)
        {
            TokenId = tokenId;
            MetadataUrl = metadataUrl;
            HashHex = hashHex;
        }
        
        /// <summary>
        /// Serialized Token Id of <see cref="CisEvent"/>. Parsed by <see cref="CommonParsers.ParseTokenId(BinaryReader)" />
        /// </summary>
        public string TokenId { get; init;  }

        public string MetadataUrl { get; init;  }
        public string? HashHex { get; init;  }

        public static CisTokenMetadataEvent Parse(ContractAddress address, BinaryReader st, string transactionHash, string? parsed)
        {
            return new CisTokenMetadataEvent(
                contractIndex: address.Index,
                contractSubIndex: address.SubIndex,
                tokenId: CommonParsers.ParseTokenId(st),
                metadataUrl: CommonParsers.ParseMetadataUrl(st),
                hashHex: st.ReadByte() == 1 ? Convert.ToHexString(st.ReadBytes(32)) : null,
                transactionHash: transactionHash,
                parsed: parsed
            );
        }

        internal override TokenEvent GetTokenEvent() => 
            new(ContractIndex, ContractSubIndex, TokenId, this);
    }
}
