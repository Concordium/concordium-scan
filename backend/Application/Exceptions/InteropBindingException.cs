namespace Application.Exceptions;

/// <summary>
/// Thrown when a interop call failed with possible error as message.
/// </summary>
internal sealed class InteropBindingException : Exception
{
    internal const string EmptyErrorMessage = "Empty error message returned";
    
    internal InteropError Error { get; }

    private InteropBindingException(string message) : base(message)
    {
        var interopError = InteropErrorExtensions.From(message);

        Error = interopError;
    }

    internal static InteropBindingException Create(string? message) => 
        message != null ? new InteropBindingException(message) : Empty();

    private static InteropBindingException Empty() => new(EmptyErrorMessage);
}

