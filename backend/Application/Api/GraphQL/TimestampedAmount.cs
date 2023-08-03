using Concordium.Sdk.Types;

namespace Application.Api.GraphQL;

public record TimestampedAmount(DateTimeOffset Timestamp, ulong Amount)
{
    internal static IEnumerable<TimestampedAmount> From(IEnumerable<(DateTimeOffset, CcdAmount)> amount)
    {
        foreach (var (dateTimeOffset, ccdAmount) in amount)
        {
            yield return new TimestampedAmount(dateTimeOffset, ccdAmount.Value);
        }
    }
}