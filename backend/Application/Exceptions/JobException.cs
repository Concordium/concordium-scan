namespace Application.Exceptions;

internal sealed class JobException : Exception
{
    private JobException(string message) : base(message)
    {}

    internal static JobException Create(string identifier, string message)
    {
        return new JobException($"Job {identifier} encountered error: {message}");
    }
}
