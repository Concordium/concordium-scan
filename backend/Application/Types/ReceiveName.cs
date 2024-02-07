using Application.Aggregates.Contract.Exceptions;
using Concordium.Sdk.Exceptions;
using Concordium.Sdk.Interop;
using Concordium.Sdk.Types;

namespace Application.Types;

internal sealed class ReceiveName
{
    private readonly Concordium.Sdk.Types.ReceiveName _receiveName;
    
    public ReceiveName(string receiveName)
    {
        if (!Concordium.Sdk.Types.ReceiveName.TryParse(receiveName, out var output))
        {
            throw new ParsingException($"Got {output!.Error} when parsing receive name {receiveName}");
        }

        _receiveName = output.ReceiveName!;
    }

    internal string? DeserializeMessage(
        string messageAsHex,
        VersionedModuleSchema schema,
        ILogger logger,
        string moduleReference,
        string instigator
        )
    {
        var contractName = _receiveName.Receive[.._receiveName.Receive.IndexOf('.')];
        var entrypoint = _receiveName.Receive[(_receiveName.Receive.IndexOf('.') + 1)..];
        try
        {
            var message = Updated.GetDeserializeMessage(schema, new ContractIdentifier(contractName),
                new EntryPoint(entrypoint), new Parameter(Convert.FromHexString(messageAsHex)));
            
            return message.ToString();   
        }
        catch (SchemaJsonException e)
        {
            Observability.ApplicationMetrics.IncInteropErrors($"{instigator}.{nameof(DeserializeMessage)}", e.SchemaJsonResult);
            switch (e.SchemaJsonResult)
            {
                case SchemaJsonResult.JsonError:
                    logger.Debug(e, "Error when parsing {Message} from {ContractName} on {Module} at {Entrypoint}", messageAsHex, contractName, moduleReference, entrypoint);
                    break;
                case SchemaJsonResult.VersionedSchemaErrorNoReceiveInContract:
                    logger.Debug(e, "{Entrypoint} not found in schema. Issue when parsing {Message} from {ContractName} on {Module}", entrypoint, messageAsHex, contractName, moduleReference);
                    break;
                case SchemaJsonResult.VersionedSchemaErrorNoParamsInReceive:
                    logger.Debug(e, "{Entrypoint} does not contain parameter in schema. Issue when parsing {Message} from {ContractName} on {Module}", entrypoint, messageAsHex, contractName, moduleReference);
                    break;
                case SchemaJsonResult.VersionedSchemaErrorNoContractInModule:
                    logger.Debug(e, "{ContractName} not in {Module}. Issue when parsing {Message} on {Entrypoint}", contractName, moduleReference, messageAsHex, entrypoint);
                    break;
                default:
                    logger.Error(e, "Error when parsing {Message} from {ContractName} on {Module} at {Entrypoint}", messageAsHex, contractName, moduleReference, entrypoint);
                    break;
            }
            return null;
        }
    }
}
