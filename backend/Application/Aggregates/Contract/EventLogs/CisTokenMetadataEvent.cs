using System.IO;
using Application.Aggregates.Contract.Entities;

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
            long transactionId, 
            string metadataUrl, 
            string? hashHex) : base(contractIndex, contractSubIndex, transactionId)
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

        public static CisTokenMetadataEvent Parse(Concordium.Sdk.Types.ContractAddress address, BinaryReader st, long transactionId)
        {
            return new CisTokenMetadataEvent(
                contractIndex: address.Index,
                contractSubIndex: address.SubIndex,
                tokenId: CommonParsers.ParseTokenId(st),
                metadataUrl: CommonParsers.ParseMetadataUrl(st),
                hashHex: st.ReadByte() == 1 ? Convert.ToHexString(st.ReadBytes(32)) : null,
                transactionId: transactionId
            );
        }

        internal override TokenEvent GetTokenEvent() => 
            new(ContractIndex, ContractSubIndex, TokenId, this);
    }
}
