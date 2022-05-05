namespace Application.Api.GraphQL.Bakers;

public record BakerStatisticsRow(
    long BakerId,
    decimal? StakePercentage,
    int? RankByStake,
    int ActiveBakerCount);