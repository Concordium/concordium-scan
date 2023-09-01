namespace Application.Aggregates.Contract.Exceptions;

/// <summary>
/// Thrown when import of data fails.
/// </summary>
public class ContractImportException : Exception
{
    public ContractImportException(string message) : base(message)
    {}
}