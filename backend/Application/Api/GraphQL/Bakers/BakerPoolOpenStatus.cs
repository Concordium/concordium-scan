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
    public static BakerPoolOpenStatus MapToGraphQlEnum(this Concordium.Sdk.Types.New.BakerPoolOpenStatus src)
    {
        return src switch
        {
            Concordium.Sdk.Types.New.BakerPoolOpenStatus.OpenForAll => BakerPoolOpenStatus.OpenForAll,
            Concordium.Sdk.Types.New.BakerPoolOpenStatus.ClosedForNew => BakerPoolOpenStatus.ClosedForNew,
            Concordium.Sdk.Types.New.BakerPoolOpenStatus.ClosedForAll => BakerPoolOpenStatus.ClosedForAll,
            _ => throw new NotImplementedException()
        };
    }
}