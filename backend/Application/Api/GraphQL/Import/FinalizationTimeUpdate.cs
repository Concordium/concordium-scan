namespace Application.Api.GraphQL.Import;

public record FinalizationTimeUpdate(long BlockHeight, DateTimeOffset BlockSlotTime, double FinalizationTimeSecs);