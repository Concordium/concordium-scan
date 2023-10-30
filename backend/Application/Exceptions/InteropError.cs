using System.Collections.Concurrent;
using Application.Interop;

namespace Application.Exceptions;

internal enum InteropError
{
    Undefined,
    EmptyMessage,
    EventNotSupported
}

internal static class InteropErrorExtensions
{
    private static readonly ConcurrentDictionary<InteropError, string> Cache = new();

    internal static string ToStringCached(this InteropError value)
    {
        return Cache.GetOrAdd(value, value.ToString());
    }
        
    internal static InteropError From(string message) =>
        message switch
        {
            "Events not supported for this module version" => InteropError.EventNotSupported,
            InteropBindingException.EmptyErrorMessage => InteropError.EmptyMessage,
            _ => InteropError.Undefined
        };

    internal static bool IsInteropErrorFatal(InteropError error) =>
        error switch
        {
            _ => false,
        };
}
