using System.Threading;
using System.Threading.Tasks;
using Application.Api.GraphQL.Bakers;
using Application.Api.GraphQL.EfCore;
using Concordium.Sdk.Client;
using Concordium.Sdk.Types;
using Microsoft.EntityFrameworkCore;

namespace Application.Database.MigrationJobs;

public class _00_FixValidatorCommissionRates : IMainMigrationJob {
    private readonly IDbContextFactory<GraphQlDbContext> _contextFactory;
    private readonly ConcordiumClient _client;

    /// <summary>
    /// WARNING - Do not change this if job already executed on environment, since it will trigger rerun of job.
    /// </summary>
    private const string JobName = "_00_FixValidatorCommissionRates";

    public _00_FixValidatorCommissionRates(
        IDbContextFactory<GraphQlDbContext> contextFactory,
        ConcordiumClient client
        )
    {
        _contextFactory = contextFactory;
        _client = client;
    }
    
    public async Task StartImport(CancellationToken token)
    {
        await using var context =  await _contextFactory.CreateDbContextAsync(token);

        await foreach (var contextBaker in context.Bakers)
        {
            if (contextBaker.State is not ActiveBakerState activeBakerState)
            {
                continue;
            }

            if (activeBakerState.Pool == null)
            {
                continue;
            }

            var poolInfo = await _client.GetPoolInfoAsync(new BakerId(new AccountIndex((ulong)contextBaker.BakerId)), new LastFinal(), token);
            
            activeBakerState.Pool.CommissionRates.Update(poolInfo.Response.PoolInfo.CommissionRates);
        }

        await context.SaveChangesAsync(token);
    }

    public string GetUniqueIdentifier() => JobName;

    public bool ShouldNodeImportAwait() => true;
}
