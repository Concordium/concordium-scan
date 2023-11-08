namespace Application.Aggregates.Contract.Exceptions;

internal sealed class ParsingException : Exception
{
    public ParsingException(string message) : base(message)
    {}
}
