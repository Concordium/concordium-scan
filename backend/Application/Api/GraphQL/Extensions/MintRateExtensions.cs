using Application.Exceptions;
using Concordium.Sdk.Types;

namespace Application.Api.GraphQL.Extensions;

internal static class MintRateExtensions
{
    internal static MintRate From(decimal number)
    {
        var initialNumber = number;
        var exponent = 0u;
        while (number != Math.Floor(number))
        {
            number *= 10.0m;
            if (number > uint.MaxValue)
            {
                throw MintRateCalculationException.MintRateExceptionWhenMantissaOverflow(initialNumber);
            }
            exponent++;
        }


        if (MintRate.TryParse(exponent, (uint)number, out var mintRate))
        {
            return mintRate!.Value;
        }
        
        throw MintRateCalculationException.MintRateExceptionWhenNotAbleToParseFromFoundExponentAndMantissa(initialNumber, exponent, number);
    }

    /// <summary>
    /// Observe - when exponent below 29 then decimal would be zero due to the precision of decimal.
    /// See https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/floating-point-numeric-types#characteristics-of-the-floating-point-types
    /// </summary>
    internal static decimal AsDecimal(this MintRate rate)
    {
        var (exponent, mantissa) = rate.GetValues();
        return mantissa * (decimal)Math.Pow(10, -1 * exponent);
    }
}