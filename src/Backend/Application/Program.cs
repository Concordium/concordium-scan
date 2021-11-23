using Application.Database;
using Application.Import.ConcordiumNode;
using Application.Import.ConcordiumNode.GrpcClient;
using Application.Persistence;

var onlyDatabaseMigration = args.FirstOrDefault()?.ToLower() == "migrate-db";

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHostedService<ImportController>();
builder.Services.AddSingleton<ConcordiumNodeGrpcClient>();
builder.Services.AddSingleton<DatabaseMigrator>();
builder.Services.AddSingleton<BlockRepository>();
builder.Services.AddSingleton(new DatabaseSettings("Host=localhost;Port=5432;Database=ConcordiumScan;"));
builder.Services.AddSingleton(new HttpClient());

var app = builder.Build();
var logger = app.Services.GetRequiredService<ILogger<Program>>();
try
{
    if (onlyDatabaseMigration)
    {
        logger.LogInformation("Application started in database migration mode. Starting database migration...");
        app.Services.GetRequiredService<DatabaseMigrator>().MigrateDatabase();
        logger.LogInformation("Database migration finished successfully");
    }
    else
    {
        logger.LogInformation("Application starting...");
        app.Services.GetRequiredService<DatabaseMigrator>().EnsureDatabaseMigrationNotNeeded();
        app.Run();    
    }
}
catch (Exception e)
{
    logger.LogError(e, "Unhandled exception caught. Terminating application.");

    // TODO: Do we need to signal to the process host that we are terminating due to an exception? throw?
}

logger.LogInformation("Exiting application!");
