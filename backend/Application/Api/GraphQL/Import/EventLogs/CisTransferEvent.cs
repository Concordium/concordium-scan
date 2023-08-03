using System.IO;
using System.Numerics;

namespace Application.Api.GraphQL.Import.EventLogs
{
    /// <summary>
    /// Represents a Token Transfer Event
    /// <see href="https://proposals.concordium.software/CIS/cis-2.html#transferevent" />
    /// </summary>
    public class CisTransferEvent : CisEvent
    {
        public CisTransferEvent() : base(CisEventType.Transfer)
        {
        }

        /// <summary>
        /// Amount of token transferred
        /// </summary>
        public BigInteger TokenAmount { get; set; }

        /// <summary>
        /// Transferred from Address
        /// </summary>
        public BaseAddress FromAddress { get; set; }

        /// <summary>
        /// Transferred to Address
        /// </summary>
        public BaseAddress ToAddress { get; set; }

        public static CisTransferEvent Parse(Concordium.Sdk.Types.ContractAddress address, BinaryReader st)
        {
            return new CisTransferEvent()
            {
                ContractIndex = address.Index,
                ContractSubIndex = address.SubIndex,
                TokenId = CommonParsers.ParseTokenId(st),
                TokenAmount = CommonParsers.ParseTokenAmount(st),
                FromAddress = CommonParsers.ParseAddress(st),
                ToAddress = CommonParsers.ParseAddress(st)
            };
        }

    }
}