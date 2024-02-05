using System.Collections.Concurrent;
using Concordium.Sdk.Interop;

namespace Application.Exceptions;

internal static class SchemaJsonResultExtensions
{
    private static readonly ConcurrentDictionary<SchemaJsonResult, string> Cache = new();

    internal static string ToStringCached(this SchemaJsonResult value)
    {
        return Cache.GetOrAdd(value, value.ToString());
    }
}
