namespace Application.Aggregates.Contract.Types;

/// <summary>
/// Specifies if data is fetched from node or existing data in database.
/// </summary>
public enum ImportSource
{
    NodeImport,
    DatabaseImport
}