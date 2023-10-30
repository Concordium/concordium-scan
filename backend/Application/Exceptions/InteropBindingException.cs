namespace Application.Exceptions;

/// <summary>
/// Thrown when a interop call failed with possible error as message.
/// </summary>
internal sealed class InteropBindingException : Exception
{
    internal const string EmptyErrorMessage = "Empty error message returned";
    
    internal bool IsFatal { get; init; }
    internal InteropError Error { get; init; }

    private InteropBindingException(string message) : base(message)
    {
        var interopError = InteropErrorExtensions.From(message);
        var isInteropErrorFatal = InteropErrorExtensions.IsInteropErrorFatal(interopError);

        Error = interopError;
        IsFatal = isInteropErrorFatal;
    }

    internal static InteropBindingException Create(string? message) => 
        message != null ? new InteropBindingException(message) : Empty();

    private static InteropBindingException Empty() => new(EmptyErrorMessage);
}

