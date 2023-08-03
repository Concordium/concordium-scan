using Application.NodeApi;
using Concordium.Sdk.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Extensions;

internal static class StartupExtensions
{
    internal static void AddConcordiumClient(this IServiceCollection services, IConfiguration configuration)
    {
        var grpcNodeClientSettings = configuration.GetSection("ConcordiumNodeGrpc").Get<GrpcNodeClientSettings>();
        var concordiumClientOptions = configuration.GetSection("ConcordiumNodeGrpc").Get<ConcordiumClientOptions>();
        var uri = new Uri(grpcNodeClientSettings.Address);
        services.AddSingleton(new ConcordiumClient(uri, concordiumClientOptions));
    }
}