namespace Application.Aggregates.Contract.Exceptions;

/// <summary>
/// Thrown when query of data fails.
/// </summary>
public class ContractQueryException : Exception
{
    public ContractQueryException(string message) : base(message)
    {}
}