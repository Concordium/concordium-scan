using System.IO;

namespace Application.Api.GraphQL.Import.EventLogs
{
    /// <summary>
    /// Base class for CIS Event. <see href="https://proposals.concordium.software/CIS/cis-2.html#abstract"/>
    /// </summary>
    public abstract class CisEvent
    {
        private static readonly List<int> AllowedEventTypes = new List<int> {
            (int)CisEventType.Burn,
            (int)CisEventType.Mint,
            (int)CisEventType.TokenMetadata,
            (int)CisEventType.Transfer,
            (int)CisEventType.UpdateOperator
        };
        
        private const int MAX_7_BIT_VALUE = 128;

        /// <summary>
        /// Instantiates a new CIS Event.
        /// </summary>
        /// <param name="type">Type of CIS Event.</param>
        public CisEvent(CisEventType type)
        {
            this.Type = type;
        }

        /// <summary>
        /// Index of Contract emitting this event.
        /// </summary>
        public ulong ContractIndex { get; init; }

        /// <summary>
        /// Sub Index of Contract emitting this event.
        /// </summary>
        /// <value></value>
        public ulong ContractSubIndex { get; init; }

        /// <summary>
        /// Type of CIS event. <see cref="CisEventType"/>
        /// </summary>
        public CisEventType Type { get; private set; }
        
        /// <summary>
        /// Serialized Token Id of <see cref="CisEvent"/>. Parsed by <see cref="CommonParsers.ParseTokenId(BinaryReader)" />
        /// </summary>
        public string TokenId { get; init; }

        /// <summary>
        /// Transaction Id of the transaction that emitted this event.
        /// </summary>
        public long TransactionId { get; init; }
        
        /// <summary>
        /// Parses CIS event bytes read from Node.
        /// </summary>
        /// <param name="eventBytes">Bytes of the event.</param>
        /// <param name="address">Contract Address emitting the event.</param>
        /// <param name="txnId">Transaction Id</param>
        /// <param name="cisEvent">Parsed Cis Event.</param>
        /// <returns></returns>
        public static bool TryParse(byte[] eventBytes, Concordium.Sdk.Types.ContractAddress address, long txnId, out CisEvent cisEvent)
        {
            if (!IsCisEvent(eventBytes))
            {
                cisEvent = null;
                return false;
            }

            try
            {
                cisEvent = CisEvent.Parse(address, eventBytes, txnId);
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
            return AllowedEventTypes.Contains((int)eventBytes.FirstOrDefault());
        }

        /// <summary>
        /// Tries to parse the input bytes or throws an Exception.
        /// </summary>
        /// <param name="address">Contract emitting the event.</param>
        /// <param name="eventBytes">Event Bytes</param>
        /// <param name="txnId">Transaction Id</param>
        /// <returns>Parsed <see cref="CisEvent"/></returns>
        private static CisEvent Parse(Concordium.Sdk.Types.ContractAddress address, byte[] eventBytes, long txnId)
        {
            var st = new BinaryReader(new MemoryStream(eventBytes));
            var eventType = st.ReadByte();
            return eventType switch
            {
                (int)CisEventType.Burn => CisBurnEvent.Parse(address, st, txnId),
                (int)CisEventType.Mint => CisMintEvent.Parse(address, st, txnId),
                (int)CisEventType.TokenMetadata => CisTokenMetadataEvent.Parse(address, st, txnId),
                (int)CisEventType.Transfer => CisTransferEvent.Parse(address, st, txnId),
                (int)CisEventType.UpdateOperator => CisUpdateOperatorEvent.Parse(address, st, txnId),
                _ => throw new Exception($"invalid event type: {eventType}")
            };
        }
    }
}
