using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using Application.Configurations;
using Application.NodeApi;
using Concordium.Sdk.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

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

    internal static void AddDefaultHealthChecks(this IApplicationBuilder app)
    {
        app.UseHealthChecks("/system/health", new HealthCheckOptions
        {
            ResponseWriter = WriteResponse
        });
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
    
    /// <summary>
    /// Taken from https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks?view=aspnetcore-7.0#customize-output
    /// such that health state is exported.
    /// </summary>
    private static Task WriteResponse(HttpContext context, HealthReport healthReport)
    {
        context.Response.ContentType = "application/json; charset=utf-8";

        var options = new JsonWriterOptions { Indented = true };

        using var memoryStream = new MemoryStream();
        using (var jsonWriter = new Utf8JsonWriter(memoryStream, options))
        {
            jsonWriter.WriteStartObject();
            jsonWriter.WriteString("status", healthReport.Status.ToString());
            jsonWriter.WriteStartObject("results");

            foreach (var healthReportEntry in healthReport.Entries)
            {
                jsonWriter.WriteStartObject(healthReportEntry.Key);
                jsonWriter.WriteString("status",
                    healthReportEntry.Value.Status.ToString());
                jsonWriter.WriteString("description",
                    healthReportEntry.Value.Description);
                jsonWriter.WriteStartObject("data");

                foreach (var item in healthReportEntry.Value.Data)
                {
                    jsonWriter.WritePropertyName(item.Key);

                    JsonSerializer.Serialize(jsonWriter, item.Value,
                        item.Value?.GetType() ?? typeof(object));
                }

                jsonWriter.WriteEndObject();
                jsonWriter.WriteEndObject();
            }

            jsonWriter.WriteEndObject();
            jsonWriter.WriteEndObject();
        }

        return context.Response.WriteAsync(
            Encoding.UTF8.GetString(memoryStream.ToArray()));
    }
}
