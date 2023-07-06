using Concordium.Sdk.Types;

namespace Application.Api.GraphQL.Extensions;

internal static class MintRateExtensions
{
    internal static bool TryParse(decimal number, out MintRate? mintRate)
    {
        var exponent = 0u;
        while (number != Math.Floor(number))
        {
            number *= 10.0m;
            if (number > uint.MaxValue)
            {
                mintRate = null;
                return false;
            }
            exponent++;
        }

        var tryParse = MintRate.TryParse(exponent, (uint)number, out mintRate);
        return tryParse;
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