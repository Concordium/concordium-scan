namespace Application.Aggregates.SmartContract.Entities;

/// <summary>
/// Specifies if data is fetched from node or existing data in database.
/// </summary>
public enum ImportSource
{
    NodeImport,
    DatabaseImport
}