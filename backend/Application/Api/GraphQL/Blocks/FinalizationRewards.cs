using Application.Api.GraphQL.EfCore;
using HotChocolate;
using HotChocolate.Data;
using HotChocolate.Types;
using Microsoft.EntityFrameworkCore;

namespace Application.Api.GraphQL.Blocks;

public class FinalizationRewards
{
    /// <summary>
    /// This property is intentionally not part of the GraphQL schema.
    /// Only here as a back reference to the owning block so that child data can be loaded.
    /// </summary>
    [GraphQLIgnore]
    public SpecialEvents Owner { get; set; }
    public ulong Remainder { get; init; }

    [UseDbContext(typeof(GraphQlDbContext))]
    [UsePaging(InferConnectionNameFromField = false)]
    public IEnumerable<FinalizationReward> GetRewards([ScopedService] GraphQlDbContext dbContext)
    {
        return dbContext.FinalizationRewards
            .AsNoTracking()
            .Where(x => x.BlockId == Owner.Owner.Id)
            .OrderBy(x => x.Index)
            .Select(x => x.Entity);
    }
}