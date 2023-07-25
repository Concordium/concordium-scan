using System.Threading.Tasks;
using Application.Api.GraphQL.EfCore;
using Application.Common.Diagnostics;
using Concordium.Sdk.Types;
using Microsoft.EntityFrameworkCore;

namespace Application.Api.GraphQL.Import;

public class IdentityProviderWriter
{
    private readonly IDbContextFactory<GraphQlDbContext> _dbContextFactory;
    private readonly IMetrics _metrics;

    public IdentityProviderWriter(IDbContextFactory<GraphQlDbContext> dbContextFactory, IMetrics metrics)
    {
        _dbContextFactory = dbContextFactory;
        _metrics = metrics;
    }

    public async Task AddGenesisIdentityProviders(IList<IpInfo> identityProviders)
    {
        await AddOrUpdate(identityProviders);
    }

    public async Task AddOrUpdateIdentityProviders(IList<BlockItemSummary> blockItemSummaries)
    {
        using var counter = _metrics.MeasureDuration(nameof(IdentityProviderWriter), nameof(AddOrUpdateIdentityProviders));

        var payloads = blockItemSummaries
            .Where(b => b.IsSuccess()) // TODO : this is not needed - keep for now to align
            .Select(b => b.Details)
            .OfType<UpdateDetails>()
            .Select(u => u.Payload)
            .OfType<AddIdentityProviderUpdate>()
            .ToArray();

        if (payloads.Length > 0)
            await AddOrUpdate(payloads.Select(x => x.IpInfo).ToArray());
    }

    public async Task AddOrUpdate(IList<IpInfo> identityProviders)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        foreach (var identityProvider in identityProviders)
        {
            var existing = await context.IdentityProviders
                .SingleOrDefaultAsync(x => x.IpIdentity == identityProvider.IpIdentity.Id);

            if (existing == null)
            {
                var mapped = new IdentityProvider(
                    checked((int)identityProvider.IpIdentity.Id),
                    identityProvider.Description.Name,
                    identityProvider.Description.Url,
                    identityProvider.Description.Info);
                
                context.IdentityProviders.Add(mapped);
            }
            else
            {
                existing.Name = identityProvider.Description.Name;
                existing.Url = identityProvider.Description.Url;
                existing.Description = identityProvider.Description.Info;
            }
        }
        await context.SaveChangesAsync();
    }
}