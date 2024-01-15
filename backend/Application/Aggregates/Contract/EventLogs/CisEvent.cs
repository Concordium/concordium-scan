using System.IO;
using Application.Aggregates.Contract.Entities;
using Application.Api.GraphQL;
using HotChocolate.Types;

namespace Application.Aggregates.Contract.EventLogs
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
        protected CisEvent(ulong contractIndex, ulong contractSubIndex, string transactionHash, string? parsed)
        {
            ContractIndex = contractIndex;
            ContractSubIndex = contractSubIndex;
            TransactionHash = transactionHash;
            Parsed = parsed;
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
        /// Transaction Hash of the transaction that emitted this event.
        /// </summary>
        public string TransactionHash { get; init; }

        /// <summary>
        /// Possible view of the event in a human interpretable form.
        /// </summary>
        public string? Parsed { get; init; }

        /// <summary>
        /// Parses CIS event bytes read from Node.
        /// </summary>
        /// <param name="eventBytes">Bytes of the event.</param>
        /// <param name="address">Contract Address emitting the event.</param>
        /// <param name="transactionHash">Transaction Hash</param>
        /// <param name="parsed">Parsed event in human interpretable form.</param>
        /// <param name="cisEvent">Parsed Cis Event.</param>
        /// <returns></returns>
        public static bool TryParse(byte[] eventBytes, ContractAddress address, string transactionHash, string? parsed, out CisEvent cisEvent)
        {
            if (!IsCisEvent(eventBytes))
            {
                cisEvent = null;
                return false;
            }

            try
            {
                cisEvent = Parse(address, eventBytes, transactionHash, parsed);
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
        /// <param name="transactionHash">Transaction Hash</param>
        /// <param name="parsed">Parsed event in human interpretable form.</param>
        /// <returns>Parsed <see cref="CisEvent"/></returns>
        private static CisEvent Parse(ContractAddress address, byte[] eventBytes, string transactionHash, string? parsed)
        {
            var st = new BinaryReader(new MemoryStream(eventBytes));
            var eventType = st.ReadByte();
            return eventType switch
            {
                (int)CisEventType.Burn => CisBurnEvent.Parse(address, st, transactionHash, parsed),
                (int)CisEventType.Mint => CisMintEvent.Parse(address, st, transactionHash, parsed),
                (int)CisEventType.TokenMetadata => CisTokenMetadataEvent.Parse(address, st, transactionHash, parsed),
                (int)CisEventType.Transfer => CisTransferEvent.Parse(address, st, transactionHash, parsed),
                (int)CisEventType.UpdateOperator => CisUpdateOperatorEvent.Parse(address, st, transactionHash, parsed),
                _ => throw new Exception($"invalid event type: {eventType}")
            };
        }
    }
}
