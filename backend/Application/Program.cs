using Application.Aggregates.Contract.Extensions;
using Application.Api.GraphQL;
using Application.Api.GraphQL.Configurations;
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
using Application.Extensions;
using Application.Import;
using Application.Import.ConcordiumNode;
using Application.Import.NodeCollector;
using Application.NodeApi;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Prometheus;
using Metrics = Application.Common.Diagnostics.Metrics;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.With<SourceClassNameEnricher>()
    .Enrich.With<TraceEnricher>()
    .CreateLogger();
builder.Logging.ClearProviders();
builder.Logging.AddSerilog(Log.Logger);
var logger = Log.ForContext<Program>();

logger.Information("Application starting...");

var featureFlags = builder.Configuration.GetSection("FeatureFlags").Get<SettingsBasedFeatureFlags>();
builder.Services.AddSingleton<IFeatureFlags>(featureFlags);
logger.Information("Feature flag [{name}]: {value}", nameof(featureFlags.MigrateDatabasesAtStartup), featureFlags.MigrateDatabasesAtStartup);
logger.Information("Feature flag [{name}]: {value}", nameof(featureFlags.ConcordiumNodeImportEnabled), featureFlags.ConcordiumNodeImportEnabled);
logger.Information("Feature flag [{name}]: {value}", nameof(featureFlags.ConcordiumNodeImportValidationEnabled), featureFlags.ConcordiumNodeImportValidationEnabled);

var nonCirculatingAccounts = builder
    .Configuration
    .GetSection("NonCirculatingAccounts")
    .Get<List<string>>()
    .Select(str => Concordium.Sdk.Types.AccountAddress.From(str).GetBaseAddress());
builder.Services.AddSingleton<NonCirculatingAccounts>(new NonCirculatingAccounts(nonCirculatingAccounts));

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

var databaseSettings = builder.Configuration.GetSection("PostgresDatabase").Get<DatabaseSettings>();
builder.Services.AddPooledDbContextFactory<GraphQlDbContext>(options =>
{
    options.UseNpgsql(databaseSettings.ConnectionString);
});
builder.Services.AddSingleton(databaseSettings);

builder.Services.AddSingleton<NodeCache>();
builder.Services.AddSingleton<IGrpcNodeCache>(x => x.GetRequiredService<NodeCache>());
builder.Services.AddSingleton<IHostedService>(x => x.GetRequiredService<NodeCache>());
builder.Services.AddSingleton<DatabaseMigrator>();
builder.Services.AddSingleton<ITimeProvider, SystemTimeProvider>();
builder.Services.AddConcordiumClient(builder.Configuration);
builder.Services.AddHttpClient<NodeCollectorClient>();
builder.Services.AddSingleton<NodeCollectorClient>();
builder.Services.AddHostedService<NodeSummaryImportController>();
builder.Services.AddSingleton<NodeStatusRepository>();
builder.Services.AddSingleton(builder.Configuration.GetSection("NodeCollectorService").Get<NodeCollectorClientSettings>());
builder.Services.AddScoped<NodeStatusSnapshot>();
builder.Services.AddHealthChecks()
    .AddCheck("Live", () => HealthCheckResult.Healthy("Application is running"))
    .ForwardToPrometheus();
builder.Services.AddContractAggregate(builder.Configuration);

builder.Host.UseSystemd();
var app = builder.Build();

try
{
    app.Services.GetRequiredService<DatabaseMigrator>().MigrateDatabases();

    app
        .Use(async (context, next) =>
        {
            context.Request.EnableBuffering();
            await next();
        })
        .UseForwardedHeaders(new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
        })
        .UseRouting()
        .UseHttpMetrics()
        .UseCors(policy =>
        {
            policy.AllowAnyOrigin();
            policy.AllowAnyHeader();
        })
        .UseWebSockets()
        .UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
            endpoints.MapGraphQL();
            endpoints.MapMetrics("/system/metrics");
        })
        .AddDefaultHealthChecks();

    app.Run();    
}
catch (Exception e)
{
    logger.Fatal(e, "Unhandled exception caught. Terminating application.");
    Environment.ExitCode = -1;
}
logger.Information("Exiting application!");
