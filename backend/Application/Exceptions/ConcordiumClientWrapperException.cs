namespace Application.Exceptions;

public sealed class ConcordiumClientWrapperException : Exception
{
    public ConcordiumClientWrapperException(string message) : base(message)
    {}
}
