namespace Application.Exceptions;

/// <summary>
/// Thrown when a interop call failed with possible error as message.
/// </summary>
internal sealed class InteropBindingException : Exception
{
    private InteropBindingException(string message) : base(message)
    {}

    internal static InteropBindingException Create(string? message) => 
        message != null ? new InteropBindingException(message) : Empty();

    private static InteropBindingException Empty() => new("Empty error message returned");
}
