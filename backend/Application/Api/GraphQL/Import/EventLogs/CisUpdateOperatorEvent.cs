using System.IO;

namespace Application.Api.GraphQL.Import.EventLogs
{
    /// <summary>
    /// Represents a Token Update Operator Event.
    /// <see href="https://proposals.concordium.software/CIS/cis-2.html#updateoperatorevent" />
    /// </summary>
    public class CisUpdateOperatorEvent : CisEvent
    {
        public CisUpdateOperatorEvent() : base(CisEventType.UpdateOperator)
        {
        }

        public OperatorUpdateType Update { get; set; }
        public BaseAddress Owner { get; set; }
        public BaseAddress Operator { get; set; }

        public static CisUpdateOperatorEvent Parse(ConcordiumSdk.Types.ContractAddress address, BinaryReader st, long txnId)
        {
            return new CisUpdateOperatorEvent()
            {
                ContractIndex = address.Index,
                ContractSubIndex = address.SubIndex,
                Update = ParseOperatorUpdate(st),
                Owner = CommonParsers.ParseAddress(st),
                Operator = CommonParsers.ParseAddress(st),
                TransactionId = txnId,
            };
        }

        private static OperatorUpdateType ParseOperatorUpdate(BinaryReader st)
        {
            var type = st.ReadByte();
            switch (type)
            {
                case 0: return OperatorUpdateType.RemoveOperator;
                case 1: return OperatorUpdateType.AddOperator;
                default: throw new Exception(String.Format("Invalid Operator update type: {0}", type));
            }
        }
    }
}