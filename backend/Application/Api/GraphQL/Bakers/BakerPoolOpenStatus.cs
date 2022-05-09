namespace Application.Api.GraphQL.Bakers;

public enum BakerPoolOpenStatus 
{
    /// <summary>
    /// New delegators may join the pool.
    /// </summary>
    OpenForAll = 0,
    /// <summary>
    /// New delegators may not join, but existing delegators are kept.
    /// </summary>
    ClosedForNew = 1,
    /// <summary>
    /// No delegators are allowed. 
    /// </summary>
    ClosedForAll = 2,
}

public static class BakerPoolOpenStatusExtensions
{
    public static BakerPoolOpenStatus MapToGraphQlEnum(this ConcordiumSdk.NodeApi.Types.BakerPoolOpenStatus src)
    {
        return src switch
        {
            ConcordiumSdk.NodeApi.Types.BakerPoolOpenStatus.OpenForAll => BakerPoolOpenStatus.OpenForAll,
            ConcordiumSdk.NodeApi.Types.BakerPoolOpenStatus.ClosedForNew => BakerPoolOpenStatus.ClosedForNew,
            ConcordiumSdk.NodeApi.Types.BakerPoolOpenStatus.ClosedForAll => BakerPoolOpenStatus.ClosedForAll,
            _ => throw new NotImplementedException()
        };
    }
}