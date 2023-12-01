namespace Application.Api.GraphQL;

public readonly record struct Ratio(ulong Numerator, ulong Denominator)
{
    internal static Ratio From(Concordium.Sdk.Types.Ratio ratio) => new(ratio.Numerator, ratio.Denominator);
}
