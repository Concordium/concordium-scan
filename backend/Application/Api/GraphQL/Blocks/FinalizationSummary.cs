using Application.Api.GraphQL.EfCore;
using HotChocolate;
using HotChocolate.Data;
using HotChocolate.Types;
using Microsoft.EntityFrameworkCore;

namespace Application.Api.GraphQL.Blocks;

public class FinalizationSummary
{
    /// <summary>
    /// This property is intentionally not part of the GraphQL schema.
    /// Only here as a back reference to the owning block so that child data can be loaded.
    /// </summary>
    [GraphQLIgnore]
    public Block Owner { get; set; }
    public string FinalizedBlockHash { get; init; }
    public long FinalizationIndex { get; init; }
    public long FinalizationDelay { get; init; }

    [UseDbContext(typeof(GraphQlDbContext))]
    [UsePaging]
    public IEnumerable<FinalizationSummaryParty> GetFinalizers([ScopedService] GraphQlDbContext dbContext)
    {
        return dbContext.FinalizationSummaryFinalizers
            .AsNoTracking()
            .Where(x => x.BlockId == Owner.Id)
            .OrderBy(x => x.Index)
            .Select(x => x.Entity);
    }
}