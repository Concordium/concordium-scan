using System.Threading.Tasks;
using Application.Api.GraphQL.EfCore;
using Application.Common.Diagnostics;
using Concordium.Sdk.Types;
using Microsoft.EntityFrameworkCore;

namespace Application.Api.GraphQL.Import;

public class ChainParametersWriter
{
    private readonly IDbContextFactory<GraphQlDbContext> _dbContextFactory;
    private readonly IMetrics _metrics;

    public ChainParametersWriter(IDbContextFactory<GraphQlDbContext> dbContextFactory, IMetrics metrics)
    {
        _dbContextFactory = dbContextFactory;
        _metrics = metrics;
    }

    public async Task<ChainParametersState> GetOrCreateChainParameters(IChainParameters chainParameters, ImportState importState)
    {
        using var counter = _metrics.MeasureDuration(nameof(ChainParametersWriter), nameof(GetOrCreateChainParameters));

        await using var context = await _dbContextFactory.CreateDbContextAsync();
        
        importState.LatestWrittenChainParameters ??= await context.ChainParameters
            .AsNoTracking()
            .OrderByDescending(x => x.Id)
            .FirstOrDefaultAsync();

        var lastWritten = importState.LatestWrittenChainParameters;
        var mappedWithLatest = ChainParameters.From(chainParameters);
        if (lastWritten != null)
        {
            if (lastWritten.Equals(mappedWithLatest))
                return new ChainParametersState(lastWritten);
        }

        var mapped = ChainParameters.From(chainParameters);
        context.ChainParameters.Add(mapped);
        await context.SaveChangesAsync();

        importState.LatestWrittenChainParameters = mapped;
        
        return lastWritten == null
            ? new ChainParametersState(mapped)
            : new ChainParametersChangedState(mapped, lastWritten);
    }
}

public record ChainParametersState(ChainParameters Current);
public record ChainParametersChangedState(ChainParameters Current, ChainParameters Previous) : ChainParametersState(Current);
