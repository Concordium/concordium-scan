namespace Application.Aggregates.SmartContract.Exceptions;

/// <summary>
/// Thrown when query of data fails.
/// </summary>
public class SmartContractQueryException : Exception
{
    public SmartContractQueryException(string message) : base(message)
    {}
}