using System.Net.Http;
using System.Threading.Tasks;
using Application;
using Application.Api.GraphQL;
using Application.Api.GraphQL.EfCore;
using Application.Api.GraphQL.Validations;
using Application.Common.FeatureFlags;
using Application.Common.Logging;
using Application.Database;
using Application.Import;
using Application.Import.ConcordiumNode;
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
var importValidationSettings = builder.Configuration.GetSection("ImportValidation").Get<ImportValidationSettings>();

logger.Information("Using Postgres connection string: {postgresConnectionString}", databaseSettings.ConnectionString);
logger.Information("Import validation enabled: {enabled}", importValidationSettings.Enabled);

builder.Services.AddMemoryCache();
builder.Services.AddCors();
builder.Services.AddGraphQLServer().Configure();
builder.Services.AddSingleton<ImportChannel>();
builder.Services.AddHostedService<ImportReadController>();
builder.Services.AddHostedService<ImportWriteController>();
builder.Services.AddSingleton<IAccountLookup, AccountLookup>();
builder.Services.AddSingleton<ImportValidationController>();
builder.Services.AddSingleton(importValidationSettings);
builder.Services.AddControllers();
builder.Services.AddPooledDbContextFactory<GraphQlDbContext>(options =>
{
    options.UseNpgsql(databaseSettings.ConnectionString);
});
builder.Services.AddSingleton<GrpcNodeClient>();
builder.Services.AddSingleton<DatabaseMigrator>();
builder.Services.AddSingleton<IFeatureFlags, SqlFeatureFlags>();
builder.Services.AddSingleton(new HttpClient());
builder.Services.AddSingleton(databaseSettings);
builder.Services.AddSingleton(builder.Configuration.GetSection("ConcordiumNodeGrpc").Get<GrpcNodeClientSettings>());
builder.Host.UseSystemd();
var app = builder.Build();

try
{
    logger.Information("Starting database migration...");
    app.Services.GetRequiredService<DatabaseMigrator>().MigrateDatabase();
    logger.Information("Database migration finished successfully");

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
