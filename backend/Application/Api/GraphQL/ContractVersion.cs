namespace Application.Api.GraphQL;

public enum ContractVersion
{
    V0,
    V1
}

internal static class ContractVersionFactory
{
    internal static ContractVersion From(Concordium.Sdk.Types.ContractVersion version)
    {
        return version switch
        {
            Concordium.Sdk.Types.ContractVersion.V0 => ContractVersion.V0,
            Concordium.Sdk.Types.ContractVersion.V1 => ContractVersion.V1,
            _ => throw new ArgumentOutOfRangeException(nameof(version), version, null)
        };
    }
}