namespace Application.Aggregates.SmartContract;

/// <summary>
/// Thrown when import of data fails.
/// </summary>
public class SmartContractImportException : Exception
{
    public SmartContractImportException(string message) : base(message)
    {}
}