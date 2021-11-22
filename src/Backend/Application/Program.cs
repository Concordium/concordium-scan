using Application.Database;
using Application.Import.ConcordiumNode;
using Application.Import.ConcordiumNode.GrpcClient;
using Application.Persistence;
using DatabaseScripts;

var databaseSettings = new DatabaseSettings("Host=localhost;Port=5432;Database=ConcordiumScan;");
var migrator = new DatabaseMigrator(databaseSettings, typeof(DatabaseScriptsMarkerType).Assembly);
if (args.FirstOrDefault()?.ToLower() == "migrate-db")
{
    migrator.MigrateDatabase();
    return;
}

migrator.EnsureDatabaseMigrationNotNeeded();

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHostedService<ImportController>();
builder.Services.AddSingleton<ConcordiumNodeGrpcClient>();
builder.Services.AddSingleton<BlockRepository>();
builder.Services.AddSingleton(databaseSettings);
builder.Services.AddSingleton(new HttpClient());

var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.Run();    
