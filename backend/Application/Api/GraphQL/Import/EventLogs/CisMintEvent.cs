using System.IO;
using System.Numerics;

namespace Application.Api.GraphQL.Import.EventLogs
{
    /// <summary>
    /// Represents a Token Mint Event
    /// <see href="https://proposals.concordium.software/CIS/cis-2.html#mintevent" />
    /// </summary>
    public class CisMintEvent : CisEvent
    {
        public CisMintEvent() : base(CisEventType.Mint)
        {
        }

        public BigInteger TokenAmount { get; set; }

        public BaseAddress ToAddress { get; set; }

        public static CisMintEvent Parse(ConcordiumSdk.Types.ContractAddress address, BinaryReader st, long txnId)
        {
            return new CisMintEvent
            {
                ContractIndex = address.Index,
                ContractSubIndex = address.SubIndex,
                TokenId = CommonParsers.ParseTokenId(st),
                TokenAmount = CommonParsers.ParseTokenAmount(st),
                ToAddress = CommonParsers.ParseAddress(st),
                TransactionId = txnId,
            };
        }

    }
}