namespace Application.Exceptions;

public sealed class MintRateCalculationException : Exception
{
    private MintRateCalculationException(decimal inputNumber, uint exponentFound, decimal mantissaFound)
        : base($"Not able to calculate mint rate from {inputNumber}, got exponent {exponentFound} " +
               $"and mantissa {mantissaFound} which could be parsed.")
    {}

    private MintRateCalculationException(decimal inputNumber) : base(
        $"Not able to calculate mint rate from {inputNumber}, mantissa grew to large.")
    {}

    internal static MintRateCalculationException MintRateExceptionWhenNotAbleToParseFromFoundExponentAndMantissa(
        decimal inputNumber,
        uint exponentFound,
        decimal mantissaFound) =>
        new(inputNumber, exponentFound, mantissaFound);

    internal static MintRateCalculationException MintRateExceptionWhenMantissaOverflow(decimal inputNumber) =>
        new(inputNumber);
}