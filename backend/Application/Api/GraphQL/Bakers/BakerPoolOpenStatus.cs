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