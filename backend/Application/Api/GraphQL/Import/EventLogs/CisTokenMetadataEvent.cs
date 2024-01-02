using System.IO;

namespace Application.Api.GraphQL.Import.EventLogs
{
    /// <summary>
    /// Represents a Token Metadata Event
    /// <see href="https://proposals.concordium.software/CIS/cis-2.html#tokenmetadataevent" />
    /// </summary>
    public class CisTokenMetadataEvent : CisEvent
    {
        public CisTokenMetadataEvent() : base(CisEventType.TokenMetadata)
        {
        }

        public string MetadataUrl { get; set; }
        public string? HashHex { get; set; }

        public static CisTokenMetadataEvent Parse(Concordium.Sdk.Types.ContractAddress address, BinaryReader st, long transactionId)
        {
            return new CisTokenMetadataEvent()
            {
                ContractIndex = address.Index,
                ContractSubIndex = address.SubIndex,
                TokenId = CommonParsers.ParseTokenId(st),
                MetadataUrl = CommonParsers.ParseMetadataUrl(st),
                HashHex = (st.ReadByte() == 1) ? Convert.ToHexString(st.ReadBytes(32)) : null,
                TransactionId = transactionId
            };
        }

    }
}
