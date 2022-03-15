namespace Application.Api.GraphQL;

public record TimestampedAmount(DateTimeOffset Timestamp, ulong Amount);