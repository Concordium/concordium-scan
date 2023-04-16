using Application.Api.GraphQL.Import.Modules;
using Application.Api.GraphQL.Modules;

public class SmartContractModuleSerDeStub : ISmartContractModuleSerDe
{
    public string? DeserializeReceiveMessage(string messageAsHex, string receiveName, ContractModuleSchema moduleSchema, int? schemaVersion = null)
    {
        return null;
    }
}