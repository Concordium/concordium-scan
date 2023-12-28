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

    [UsePaging]
    public IEnumerable<FinalizationSummaryParty> GetFinalizers(GraphQlDbContext dbContext)
    {
        return dbContext.FinalizationSummaryFinalizers
            .AsNoTracking()
            .Where(x => x.BlockId == Owner.Id)
            .OrderBy(x => x.Index)
            .Select(x => x.Entity);
    }
    
    internal static bool TryMapFinalizationSummary(
        Concordium.Sdk.Types.FinalizationSummary? data,
        out FinalizationSummary? finalizationSummary)
    {
        if (data == null)
        {
            finalizationSummary = null;
            return false;
        };
        finalizationSummary = new FinalizationSummary
        {
            FinalizedBlockHash = data.BlockPointer.ToString(),
            FinalizationIndex = (long)data.Index,
            FinalizationDelay = (long)data.Delay,
        };
        return true;
    }
}
