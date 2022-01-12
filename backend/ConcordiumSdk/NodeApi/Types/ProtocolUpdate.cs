using ConcordiumSdk.Types;

namespace ConcordiumSdk.NodeApi.Types;

public record ProtocolUpdate(
    string Message,
    string SpecificationURL,
    string SpecificationHash,
    BinaryData SpecificationAuxiliaryData);