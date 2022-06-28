using System.Net.Http;
using Application;
using Application.Api.GraphQL;
using Application.Api.GraphQL.Bakers;
using Application.Api.GraphQL.EfCore;
using Application.Api.GraphQL.Import;
using Application.Api.GraphQL.Import.Validations;
using Application.Api.GraphQL.ImportNodeStatus;
using Application.Api.GraphQL.Network;
using Application.Common;
using Application.Common.Diagnostics;
using Application.Common.FeatureFlags;
using Application.Common.Logging;
using Application.Database;
using Application.Import;
using Application.Import.ConcordiumNode;
using Application.Import.NodeCollector;
using ConcordiumSdk.NodeApi;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

if (args.Any())
{
    await SpecialRunModeHandler.HandleCommandLineArgs(args);
    return;
}
var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.With<SourceClassNameEnricher>()    
    .CreateLogger();
builder.Logging.ClearProviders();
builder.Logging.AddSerilog(Log.Logger);
var logger = Log.ForContext<Program>();

logger.Information("Application starting...");

var databaseSettings = builder.Configuration.GetSection("PostgresDatabase").Get<DatabaseSettings>();
logger.Information("Using Postgres connection string: {postgresConnectionString}", databaseSettings.ConnectionString);

var featureFlags = builder.Configuration.GetSection("FeatureFlags").Get<SettingsBasedFeatureFlags>();
builder.Services.AddSingleton<IFeatureFlags>(featureFlags);
logger.Information("Feature flag [{name}]: {value}", nameof(featureFlags.MigrateDatabasesAtStartup), featureFlags.MigrateDatabasesAtStartup);
logger.Information("Feature flag [{name}]: {value}", nameof(featureFlags.ConcordiumNodeImportEnabled), featureFlags.ConcordiumNodeImportEnabled);
logger.Information("Feature flag [{name}]: {value}", nameof(featureFlags.ConcordiumNodeImportValidationEnabled), featureFlags.ConcordiumNodeImportValidationEnabled);

builder.Services.AddMemoryCache();
builder.Services.AddCors();
builder.Services.AddGraphQLServer().Configure();
builder.Services.AddSingleton<IMetrics, Metrics>();
builder.Services.AddSingleton<MetricsListener>();
builder.Services.AddSingleton<ImportChannel>();
builder.Services.AddHostedService<ImportReadController>();
builder.Services.AddHostedService<ImportWriteController>();
builder.Services.AddHostedService<BlockAddedPublisher>();
builder.Services.AddSingleton<IAccountLookup, AccountLookup>();
builder.Services.AddSingleton<ImportValidationController>();
builder.Services.AddControllers();
builder.Services.AddPooledDbContextFactory<GraphQlDbContext>(options =>
{
    options.UseNpgsql(databaseSettings.ConnectionString);
});
builder.Services.AddSingleton<NodeCache>();
builder.Services.AddSingleton<IGrpcNodeCache>(x => x.GetRequiredService<NodeCache>());
builder.Services.AddSingleton<IHostedService>(x => x.GetRequiredService<NodeCache>());
builder.Services.AddSingleton<GrpcNodeClient>();
builder.Services.AddSingleton<DatabaseMigrator>();
builder.Services.AddSingleton<ITimeProvider, SystemTimeProvider>();
builder.Services.AddSingleton(new HttpClient());
builder.Services.AddSingleton(databaseSettings);
builder.Services.AddSingleton(builder.Configuration.GetSection("ConcordiumNodeGrpc").Get<GrpcNodeClientSettings>());
builder.Services.AddSingleton<NodeCollectorClient>();
builder.Services.AddHostedService<NodeSummaryImportController>();
builder.Services.AddSingleton<NodeStatusRepository>();
builder.Services.AddSingleton(builder.Configuration.GetSection("NodeCollectorService").Get<NodeCollectorClientSettings>());
builder.Services.AddScoped<NodeStatusSnapshot>();
builder.Host.UseSystemd();
var app = builder.Build();

try
{
    app.Services.GetRequiredService<DatabaseMigrator>().MigrateDatabases();

    app
        .UseForwardedHeaders(new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
        })
        .UseRouting()
        .UseWebSockets()
        .UseCors(policy =>
        {
            policy.AllowAnyOrigin();
            policy.AllowAnyHeader();
        })
        .UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
            endpoints.MapGraphQL();
        });
    
    app.Run();    
}
catch (Exception e)
{
    logger.Fatal(e, "Unhandled exception caught. Terminating application.");
    Environment.ExitCode = -1;
}
logger.Information("Exiting application!");