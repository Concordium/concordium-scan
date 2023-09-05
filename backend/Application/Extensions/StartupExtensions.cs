using System.Runtime.CompilerServices;
using Application.Configurations;
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
    
    internal static IServiceCollection AddFeatureFlagOptions(this IServiceCollection collection, IConfiguration configuration, ILogger logger)
    {
        var featureFlags = configuration.GetSection("FeatureFlags").Get<FeatureFlagOptions>();
        collection.Configure<FeatureFlagOptions>(configuration.GetSection("FeatureFlags"));
        logger.Information("Feature flag [{name}]: {value}", nameof(featureFlags.MigrateDatabasesAtStartup), featureFlags.MigrateDatabasesAtStartup);
        logger.Information("Feature flag [{name}]: {value}", nameof(featureFlags.ConcordiumNodeImportEnabled), featureFlags.ConcordiumNodeImportEnabled);
        logger.Information("Feature flag [{name}]: {value}", nameof(featureFlags.ConcordiumNodeImportValidationEnabled), featureFlags.ConcordiumNodeImportValidationEnabled);
        return collection;
    }
}