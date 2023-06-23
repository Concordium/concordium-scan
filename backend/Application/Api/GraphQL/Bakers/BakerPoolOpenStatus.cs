namespace Application.Api.GraphQL.Bakers;

/// <summary>
/// The status of whether a baking pool allows delegators to join.
/// </summary>
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
    public static BakerPoolOpenStatus MapToGraphQlEnum(this Concordium.Sdk.Types.BakerPoolOpenStatus src)
    {
        return src switch
        {
            Concordium.Sdk.Types.BakerPoolOpenStatus.OpenForAll => BakerPoolOpenStatus.OpenForAll,
            Concordium.Sdk.Types.BakerPoolOpenStatus.ClosedForNew => BakerPoolOpenStatus.ClosedForNew,
            Concordium.Sdk.Types.BakerPoolOpenStatus.ClosedForAll => BakerPoolOpenStatus.ClosedForAll,
            _ => throw new NotImplementedException()
        };
    }
}