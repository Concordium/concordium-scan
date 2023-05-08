using System.Numerics;
using Application.Api.GraphQL.Import.EventLogs;
using HotChocolate.Types;

namespace Application.Api.GraphQL.Tokens
{
    [UnionType("CisEvent")]
    public abstract record CisEventData
    {
    }

    public record CisEventDataBurn : CisEventData
    {
        public string Amount { get; set; }
        public Address From { get; set; }
    }

    public record CisEventDataMetadataUpdate : CisEventData
    {
        public string MetadataUrl { get; set; }
        public string? MetadataHashHex { get; set; }
    }

    public record CisEventDataMint : CisEventData
    {
        public string Amount { get; set; }
        public Address To { get; set; }
    }

    public record CisEventDataTransfer : CisEventData
    {
        public string Amount { get; set; }
        public Address From { get; set; }
        public Address To { get; set; }
    }
}