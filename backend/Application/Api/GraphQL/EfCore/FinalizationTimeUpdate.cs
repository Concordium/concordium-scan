namespace Application.Api.GraphQL.EfCore;

public record FinalizationTimeUpdate(long BlockHeight, DateTimeOffset BlockSlotTime, double FinalizationTimeSecs);