using Application.Import.ConcordiumNode;
using Application.Import.ConcordiumNode.GrpcClient;
using Application.Persistence;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHostedService<ImportController>();
builder.Services.AddSingleton<ConcordiumNodeGrpcClient>();
builder.Services.AddSingleton<BlockRepository>();
builder.Services.AddSingleton(new HttpClient());

var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.Run();