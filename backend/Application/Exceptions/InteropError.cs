using System.Collections.Concurrent;

namespace Application.Exceptions;

internal enum InteropError
{
    Undefined,
    EmptyMessage,
    EventNotSupported,
    Deserialization,
    NoReceiveInContract,
    NoParamsInReceive,
    NoContractInModule
}

internal static class InteropErrorExtensions
{
    private static readonly ConcurrentDictionary<InteropError, string> Cache = new();

    internal static string ToStringCached(this InteropError value)
    {
        return Cache.GetOrAdd(value, value.ToString());
    }

    internal static InteropError From(string message)
    {
        // Failed to deserialize.
        // https://github.com/Concordium/concordium-base/blob/0fbae087195589bce53d39944d340c7df5106d64/smart-contracts/contracts-common/concordium-contracts-common/src/schema_json.rs#L377
        // This is possible a `ParseError` 
        // https://github.com/Concordium/concordium-rust-smart-contracts/blob/673d09236b40e4583e60b8aa2cd7b6849b1c6189/concordium-std/src/lib.rs#L202
        // mapped to a custom error like
        // https://github.com/Concordium/concordium-rust-smart-contracts/blob/673d09236b40e4583e60b8aa2cd7b6849b1c6189/examples/cis2-wccd/src/lib.rs#L211
        if (message.StartsWith("Failed to deserialize"))
        {
            return InteropError.Deserialization;
        }
        
        return message switch
        {
            // https://github.com/Concordium/concordium-base/blob/0fbae087195589bce53d39944d340c7df5106d64/smart-contracts/contracts-common/concordium-contracts-common/src/schema.rs#L1062
            "Events not supported for this module version" => InteropError.EventNotSupported,
            // Versioned Schema Error
            // https://github.com/Concordium/concordium-base/blob/0fbae087195589bce53d39944d340c7df5106d64/smart-contracts/contracts-common/concordium-contracts-common/src/schema.rs#L1043
            "Receive function schema not found in contract schema" => InteropError.NoReceiveInContract,
            // Versioned Schema Error
            // https://github.com/Concordium/concordium-base/blob/0fbae087195589bce53d39944d340c7df5106d64/smart-contracts/contracts-common/concordium-contracts-common/src/schema.rs#L1047C17-L1047C17
            "Receive function schema does not contain a parameter schema" => InteropError.NoParamsInReceive,
            // Versioned Schema Error
            // https://github.com/Concordium/concordium-base/blob/0fbae087195589bce53d39944d340c7df5106d64/smart-contracts/contracts-common/concordium-contracts-common/src/schema.rs#L1041
            "Unable to find contract schema in module schema" => InteropError.NoContractInModule,
            InteropBindingException.EmptyErrorMessage => InteropError.EmptyMessage,
            _ => InteropError.Undefined
        };
    }

    internal static bool IsInteropErrorFatal(InteropError error) =>
        error switch
        {
            _ => false,
        };
}