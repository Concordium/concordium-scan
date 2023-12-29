using HotChocolate.Types;

namespace Application.Api.GraphQL.Tokens;

[UnionType("CisEvent")]
public abstract record CisEventData
{
}

public record CisEventDataBurn(string Amount, Address From) : CisEventData;

public record CisEventDataMetadataUpdate(string MetadataUrl, string? MetadataHashHex) : CisEventData;

public record CisEventDataMint(string Amount, Address To) : CisEventData;

public record CisEventDataTransfer(string Amount, Address From, Address To) : CisEventData;
