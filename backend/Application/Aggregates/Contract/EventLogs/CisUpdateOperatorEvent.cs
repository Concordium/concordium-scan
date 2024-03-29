using System.IO;
using Application.Aggregates.Contract.Entities;
using Application.Api.GraphQL;

namespace Application.Aggregates.Contract.EventLogs
{
    /// <summary>
    /// Represents a Token Update Operator Event.
    /// <see href="https://proposals.concordium.software/CIS/cis-2.html#updateoperatorevent" />
    /// </summary>
    public class CisUpdateOperatorEvent : CisEvent
    {
        public CisUpdateOperatorEvent(            
            ulong contractIndex,
            ulong contractSubIndex,
            string transactionHash,
            string? parsed,
            OperatorUpdateType update,
            Address owner,
            Address @operator) : base(contractIndex, contractSubIndex, transactionHash, parsed)
        {
            Update = update;
            Owner = owner;
            Operator = @operator;
        }

        public OperatorUpdateType Update { get; init; }
        public Address Owner { get; init; }
        public Address Operator { get; init; }

        public static CisUpdateOperatorEvent Parse(ContractAddress address, BinaryReader st, string transactionHash, string? parsed)
        {
            return new CisUpdateOperatorEvent(
                contractIndex: address.Index,
                contractSubIndex: address.SubIndex,
                update: ParseOperatorUpdate(st),
                owner: CommonParsers.ParseAddress(st),
                @operator: CommonParsers.ParseAddress(st),
                transactionHash: transactionHash,
                parsed: parsed
            );
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

        internal override TokenEvent? GetTokenEvent() => null;
    }
}
