using System.Collections.Concurrent;

namespace Application.Aggregates.Contract.Types;

/// <summary>
/// Specifies if data is fetched from node or existing data in database.
/// </summary>
public enum ImportSource
{
    NodeImport,
    DatabaseImport
}

internal static class ImportSourceExtensions
{
    private static readonly ConcurrentDictionary<ImportSource, string> Cache = new();

    internal static string ToStringCached(this ImportSource value)
    {
        return Cache.GetOrAdd(value, value.ToString());
    }
}
