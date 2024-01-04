using System.IO;
using Application.Api.GraphQL.Tokens;
using HotChocolate.Types;

namespace Application.Api.GraphQL.Import.EventLogs
{
    /// <summary>
    /// Base class for CIS Event. <see href="https://proposals.concordium.software/CIS/cis-2.html#abstract"/>
    /// </summary>
    [UnionType("CisEvent")]
    public abstract class CisEvent
    {
        private static readonly List<int> AllowedEventTypes = new()
        {
            (int)CisEventType.Burn,
            (int)CisEventType.Mint,
            (int)CisEventType.TokenMetadata,
            (int)CisEventType.Transfer,
            (int)CisEventType.UpdateOperator
        };
        
        internal abstract TokenEvent? GetTokenEvent();

        /// <summary>
        /// Instantiates a new CIS Event.
        /// </summary>
        protected CisEvent(ulong contractIndex, ulong contractSubIndex, long transactionId)
        {
            ContractIndex = contractIndex;
            ContractSubIndex = contractSubIndex;
            TransactionId = transactionId;
        }

        /// <summary>
        /// Index of Contract emitting this event.
        /// </summary>
        public ulong ContractIndex { get; init; }

        /// <summary>
        /// Sub Index of Contract emitting this event.
        /// </summary>
        /// <value></value>
        public ulong ContractSubIndex { get; init;  }

        /// <summary>
        /// Transaction Id of the transaction that emitted this event.
        /// </summary>
        public long TransactionId { get; init; }
        
        /// <summary>
        /// Parses CIS event bytes read from Node.
        /// </summary>
        /// <param name="eventBytes">Bytes of the event.</param>
        /// <param name="address">Contract Address emitting the event.</param>
        /// <param name="transactionId">Transaction Id</param>
        /// <param name="cisEvent">Parsed Cis Event.</param>
        /// <returns></returns>
        public static bool TryParse(byte[] eventBytes, Concordium.Sdk.Types.ContractAddress address, long transactionId, out CisEvent cisEvent)
        {
            if (!IsCisEvent(eventBytes))
            {
                cisEvent = null;
                return false;
            }

            try
            {
                cisEvent = CisEvent.Parse(address, eventBytes, transactionId);
                return true;
            }
            catch (Exception)
            {
                cisEvent = null;
                return false;
            }
        }

        /// <summary>
        /// Checks wether the input bytes is a CIS event by checking the first byte.
        /// </summary>
        /// <param name="eventBytes">Input bytes</param>
        /// <returns></returns>
        public static bool IsCisEvent(byte[] eventBytes)
        {
            return AllowedEventTypes.Contains(eventBytes.FirstOrDefault());
        }

        /// <summary>
        /// Tries to parse the input bytes or throws an Exception.
        /// </summary>
        /// <param name="address">Contract emitting the event.</param>
        /// <param name="eventBytes">Event Bytes</param>
        /// <param name="transactionId">Transaction Id</param>
        /// <returns>Parsed <see cref="CisEvent"/></returns>
        private static CisEvent Parse(Concordium.Sdk.Types.ContractAddress address, byte[] eventBytes, long transactionId)
        {
            var st = new BinaryReader(new MemoryStream(eventBytes));
            var eventType = st.ReadByte();
            return eventType switch
            {
                (int)CisEventType.Burn => CisBurnEvent.Parse(address, st, transactionId),
                (int)CisEventType.Mint => CisMintEvent.Parse(address, st, transactionId),
                (int)CisEventType.TokenMetadata => CisTokenMetadataEvent.Parse(address, st, transactionId),
                (int)CisEventType.Transfer => CisTransferEvent.Parse(address, st, transactionId),
                (int)CisEventType.UpdateOperator => CisUpdateOperatorEvent.Parse(address, st, transactionId),
                _ => throw new Exception($"invalid event type: {eventType}")
            };
        }
    }
}
