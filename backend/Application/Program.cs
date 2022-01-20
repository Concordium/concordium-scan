using System.Net.Http;
using Application.Api.GraphQL;
using Application.Api.GraphQL.EfCore;
using Application.Common.FeatureFlags;
using Application.Common.Logging;
using Application.Database;
using Application.Import.ConcordiumNode;
using Application.Persistence;
using ConcordiumSdk.NodeApi;
using HotChocolate.Types;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

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

builder.Services.AddCors();
builder.Services.AddGraphQLServer().AddQueryType<Query>()
    .AddType<AccountTransaction>().AddType<CredentialDeploymentTransaction>().AddType<UpdateTransaction>()
    .AddType<Successful>().AddType<Rejected>()
    .AddType<AccountCreated>().AddType<AmountAddedByDecryption>().AddType<BakerAdded>().AddType<BakerKeysUpdated>()
    .AddType<BakerRemoved>().AddType<BakerSetRestakeEarnings>().AddType<BakerStakeDecreased>()
    .AddType<BakerStakeIncreased>().AddType<ContractInitialized>().AddType<CredentialDeployed>()
    .AddType<CredentialKeysUpdated>().AddType<CredentialsUpdated>().AddType<DataRegistered>()
    .AddType<EncryptedAmountsRemoved>().AddType<EncryptedSelfAmountAdded>().AddType<ContractModuleDeployed>()
    .AddType<NewEncryptedAmount>().AddType<TransferMemo>().AddType<Transferred>().AddType<TransferredWithSchedule>()
    .AddType<ChainUpdateEnqueued>().AddType<ContractUpdated>()
    .AddType<ContractAddress>().AddType<AccountAddress>()
    .AddType<AlreadyABaker>()
    .AddType<AmountTooLarge>()
    .AddType<BakerInCooldown>()
    .AddType<CredentialHolderDidNotSign>()
    .AddType<DuplicateAggregationKey>()
    .AddType<DuplicateCredIds>()
    .AddType<EncryptedAmountSelfTransfer>()
    .AddType<FirstScheduledReleaseExpired>()
    .AddType<InsufficientBalanceForBakerStake>()
    .AddType<InvalidAccountReference>()
    .AddType<InvalidAccountThreshold>()
    .AddType<InvalidContractAddress>()
    .AddType<InvalidCredentialKeySignThreshold>()
    .AddType<InvalidCredentials>()
    .AddType<InvalidEncryptedAmountTransferProof>()
    .AddType<InvalidIndexOnEncryptedTransfer>()
    .AddType<InvalidInitMethod>()
    .AddType<InvalidModuleReference>()
    .AddType<InvalidProof>()
    .AddType<InvalidReceiveMethod>()
    .AddType<InvalidTransferToPublicProof>()
    .AddType<KeyIndexAlreadyInUse>()
    .AddType<ModuleHashAlreadyExists>()
    .AddType<ModuleNotWf>()
    .AddType<NonExistentCredIds>()
    .AddType<NonExistentCredentialId>()
    .AddType<NonExistentRewardAccount>()
    .AddType<NonIncreasingSchedule>()
    .AddType<NotABaker>()
    .AddType<NotAllowedMultipleCredentials>()
    .AddType<NotAllowedToHandleEncrypted>()
    .AddType<NotAllowedToReceiveEncrypted>()
    .AddType<OutOfEnergy>()
    .AddType<RejectedInit>()
    .AddType<RejectedReceive>()
    .AddType<RemoveFirstCredential>()
    .AddType<RuntimeFailure>()
    .AddType<ScheduledSelfTransfer>()
    .AddType<SerializationFailure>()
    .AddType<StakeUnderMinimumThresholdForBaking>()
    .AddType<ZeroScheduledAmount>()
    .BindRuntimeType<ulong, UnsignedLongType>();
             
builder.Services.AddHostedService<ImportController>();
builder.Services.AddControllers();
builder.Services.AddPooledDbContextFactory<GraphQlDbContext>(options =>
{
    options.UseNpgsql(databaseSettings.ConnectionString);
});
builder.Services.AddSingleton<DataUpdateController>();
builder.Services.AddSingleton<GrpcNodeClient>();
builder.Services.AddSingleton<DatabaseMigrator>();
builder.Services.AddSingleton<IFeatureFlags, SqlFeatureFlags>();
builder.Services.AddSingleton<BlockRepository>();
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