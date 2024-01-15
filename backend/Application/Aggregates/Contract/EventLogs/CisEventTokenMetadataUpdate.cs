namespace Application.Aggregates.Contract.EventLogs
{
    /// <summary>
    /// Represents a update related to Token Metadata.
    /// </summary>
    public class CisEventTokenMetadataUpdate : CisEventTokenUpdate
    {
        public string MetadataUrl { get; set; }

        public string? HashHex { get; set; }
    }
}