using System.Threading;
using System.Threading.Tasks;
using Application.Api.GraphQL.Bakers;
using Application.Api.GraphQL.EfCore;
using Application.Import.ConcordiumNode;
using Application.Resilience;
using Concordium.Sdk.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Application.Database.MigrationJobs;

/// <summary>
/// Some validators have not have their commission rates updated whenever a chain update would affect their commissions.
/// This is due to a bug where commission rates from chain updates was not compared correctly.
/// This job loop through all bakers with commission rates set, and updates those values to data fetched directly
/// from the chain.
/// </summary>
public class _00_UpdateValidatorCommissionRates : IMainMigrationJob {
    /// <summary>
    /// WARNING - Do not change this if job already executed on environment, since it will trigger rerun of job.
    /// </summary>
    private const string JobName = "_00_UpdateValidatorCommissionRates";
    private readonly IDbContextFactory<GraphQlDbContext> _contextFactory;
    private readonly IConcordiumNodeClient _client;
    private readonly ILogger _logger;
    private readonly MainMigrationJobOptions _mainMigrationJobOptions;

    public _00_UpdateValidatorCommissionRates(
        IDbContextFactory<GraphQlDbContext> contextFactory,
        IConcordiumNodeClient client,
        IOptions<MainMigrationJobOptions> options
        )
    {
        _contextFactory = contextFactory;
        _client = client;
        _logger = Log.ForContext<_00_UpdateValidatorCommissionRates>();
        _mainMigrationJobOptions = options.Value;
    }
    
    public async Task StartImport(CancellationToken token)
    {
        await Policies.GetTransientPolicy(GetUniqueIdentifier(), _logger, _mainMigrationJobOptions.RetryCount, _mainMigrationJobOptions.RetryDelay)
            .ExecuteAsync(async () =>
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
            
                    activeBakerState.Pool.CommissionRates.Update(poolInfo.PoolInfo.CommissionRates);
                }

                await context.SaveChangesAsync(token);
            });
    }

    public string GetUniqueIdentifier() => JobName;

    public bool ShouldNodeImportAwait() => true;
}
