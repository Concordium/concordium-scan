using ConcordiumSdk.Types;

namespace ConcordiumSdk.NodeApi.Types;

public record ContractInstanceInfo(
    BinaryData Model,
    AccountAddress Owner,
    CcdAmount Amount,
    string[] Methods,
    string Name,
    ModuleRef SourceModule);