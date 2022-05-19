namespace Application.Api.GraphQL.Bakers;

public record BakerStatisticsRow(
    long BakerId,
    decimal? PoolTotalStakePercentage,
    int? PoolRankByTotalStake,
    int ActiveBakerPoolCount);