using System.Threading.Tasks;
using Application.Api.GraphQL.EfCore;
using Application.Common.Diagnostics;
using Application.NodeApi;
using Concordium.Sdk.Types.New;
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

    public async Task AddGenesisIdentityProviders(IdentityProviderInfo[] identityProviders)
    {
        await AddOrUpdate(identityProviders);
    }

    public async Task AddOrUpdateIdentityProviders(TransactionSummary[] transactionSummaries)
    {
        using var counter = _metrics.MeasureDuration(nameof(IdentityProviderWriter), nameof(AddOrUpdateIdentityProviders));

        var payloads = transactionSummaries
            .Where(x => x.Type.Equals(TransactionType.Get(UpdateTransactionType.UpdateAddIdentityProvider)))
            .Select(x => x.Result).OfType<TransactionSuccessResult>()
            .SelectMany(x => x.Events).OfType<UpdateEnqueued>()
            .Select(x => x.Payload).OfType<AddIdentityProviderUpdatePayload>()
            .ToArray();

        if (payloads.Length > 0)
            await AddOrUpdate(payloads.Select(x => x.Content).ToArray());
    }

    public async Task AddOrUpdate(IdentityProviderInfo[] identityProviders)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        foreach (var identityProvider in identityProviders)
        {
            var existing = await context.IdentityProviders
                .SingleOrDefaultAsync(x => x.IpIdentity == identityProvider.IpIdentity);

            if (existing == null)
            {
                var mapped = new IdentityProvider(
                    Convert.ToInt32(identityProvider.IpIdentity),
                    identityProvider.IpDescription.Name,
                    identityProvider.IpDescription.Url,
                    identityProvider.IpDescription.Description);
                
                context.IdentityProviders.Add(mapped);
            }
            else
            {
                existing.Name = identityProvider.IpDescription.Name;
                existing.Url = identityProvider.IpDescription.Url;
                existing.Description = identityProvider.IpDescription.Description;
            }
        }
        await context.SaveChangesAsync();
    }
}