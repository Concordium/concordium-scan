using System.IO;
using System.Numerics;
using NBitcoin.DataEncoders;

namespace Application.Api.GraphQL.Import.EventLogs
{
    /// <summary>
    /// Base class for CIS Event. <see href="https://proposals.concordium.software/CIS/cis-2.html#abstract"/>
    /// </summary>
    public abstract class CisEvent
    {
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
        public static bool TryParse(
            byte[] eventBytes,
            ConcordiumSdk.Types.ContractAddress address,
            long txnId,
            out CisEvent cisEvent)
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
            catch (System.Exception ex)
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
            var allowedEventTypes = new List<int> {
                (int)CisEventType.Burn,
                (int)CisEventType.Mint,
                (int)CisEventType.TokenMetadata,
                (int)CisEventType.Transfer,
                (int)CisEventType.UpdateOperator
            };

            return allowedEventTypes.Contains((int)eventBytes.FirstOrDefault());
        }

        /// <summary>
        /// Tries to parse the input bytes or throws an Exception.
        /// </summary>
        /// <param name="address">Contract emitting the event.</param>
        /// <param name="eventBytes">Event Bytes</param>
        /// <returns>Parsed <see cref="CisEvent"/></returns>
        private static CisEvent Parse(ConcordiumSdk.Types.ContractAddress address, byte[] eventBytes, long txnId)
        {
            var st = new BinaryReader(new MemoryStream(eventBytes));
            var eventType = st.ReadByte();
            switch (eventType)
            {
                case ((int)CisEventType.Burn):
                    return CisBurnEvent.Parse(address, st, txnId);
                case ((int)CisEventType.Mint):
                    return CisMintEvent.Parse(address, st, txnId);
                case ((int)CisEventType.TokenMetadata):
                    return CisTokenMetadataEvent.Parse(address, st, txnId);
                case ((int)CisEventType.Transfer):
                    return CisTransferEvent.Parse(address, st, txnId);
                case ((int)CisEventType.UpdateOperator):
                    return CisUpdateOperatorEvent.Parse(address, st, txnId);
                default:
                    throw new Exception(String.Format("invalid event type: {0}", eventType));
            }
        }
    }
}