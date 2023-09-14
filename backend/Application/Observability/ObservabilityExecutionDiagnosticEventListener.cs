using System.IO;
using System.Text;
using HotChocolate;
using HotChocolate.Execution;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Execution.Processing;
using HotChocolate.Resolvers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.ObjectPool;

namespace Application.Observability;

/// <summary>
/// This class adds diagnostics and hence metrics to the GraphQl request flow, see <see cref="HotChocolate.Execution.IRequestContext"/>.
/// 
/// Diagnostics are stored in <see cref="HotChocolate.IHasContextData.ContextData"/> and can then be enriched
/// throughout the request.
///
/// Example if errors are reported, any diagnostics can be enriched by accessing the diagnostic in the storage.
///
/// When the request finalize all diagnostics will be disposed. 
/// </summary>
public sealed class ObservabilityExecutionDiagnosticEventListener : ExecutionDiagnosticEventListener
{
    private const string HttpContext = "HttpContext";
    private const string DefaultNoQueryResponse = "No able to get Query";
    
    private readonly ILogger _logger;

    /// <summary>
    /// Gets the <see cref="StringBuilder"/> pool used by this enricher.
    /// </summary>
    private ObjectPool<StringBuilder> StringBuilderPool { get; }
    
    public ObservabilityExecutionDiagnosticEventListener()
    {
        StringBuilderPool = ObjectPool.Create<StringBuilder>();
        _logger = Log.ForContext<ObservabilityExecutionDiagnosticEventListener>();
    }

    public override IDisposable ExecuteRequest(IRequestContext context)
    {
        var durationMetric = new ApplicationMetrics.GraphQlDurationMetric(context, StringBuilderPool);
        
        context.ContextData[ApplicationMetrics.GraphQlDurationMetric.GraphQlDurationMetricContextKey] =
            durationMetric;

        return durationMetric;
    }

    /// <summary>
    /// Exceptions occurs when ex. client refresh browser and hence cancel request.
    /// </summary>
    public override void RequestError(IRequestContext context, Exception exception)
    {
        if (context.ContextData.TryGetValue(
                ApplicationMetrics.GraphQlDurationMetric.GraphQlDurationMetricContextKey,
                out var metric) &&
            metric is ApplicationMetrics.GraphQlDurationMetric graphQlDurationMetric)
        {
            graphQlDurationMetric.SetException(exception);
        }
        if (!ShouldLogException(exception))
        {
            return;
        }

        var query = GetQuery(context);
        
        _logger.Error(exception, "Exception from {Query}", query);
    }
    
    /// <summary>
    /// Exceptions from resolver execution.
    /// </summary>
    public override void ResolverError(IMiddlewareContext context, IError error)
    {
        if (error.Exception == null)
        {
            return;
        }
        if (context.ContextData.TryGetValue(
                ApplicationMetrics.GraphQlDurationMetric.GraphQlDurationMetricContextKey,
                out var metric) &&
            metric is ApplicationMetrics.GraphQlDurationMetric graphQlDurationMetric)
        {
            graphQlDurationMetric.SetException(error.Exception);
        }
        if (!ShouldLogException(error.Exception))
        {
            return;
        }

        var query = GetQuery(context);

        _logger.Error(error.Exception, "Exception from {Query}", query);
    }

    public override void ResolverError(IRequestContext context, ISelection selection, IError error)
    {
        if (error.Exception is null)
        {
            return;
        }
        if (context.ContextData.TryGetValue(
                ApplicationMetrics.GraphQlDurationMetric.GraphQlDurationMetricContextKey,
                out var metric) &&
            metric is ApplicationMetrics.GraphQlDurationMetric graphQlDurationMetric)
        {
            graphQlDurationMetric.SetException(error.Exception);
        }
        if (!ShouldLogException(error.Exception))
        {
            return;
        }
        
        var query = GetQuery(context);
        
        _logger.Error(error.Exception, "Exception from {Query}", query);
    }
    
    private static bool ShouldLogException(Exception? exception)
    {
        if (exception is null)
        {
            return false;
        }
        return exception switch
        {
            // Don't log when users cancel queries
            OperationCanceledException => false,
            ObjectDisposedException => false,
            _ => true,
        };
    }
    
    private static string GetQuery(IRequestContext context)
    {
        if (TryGetQuery(context, out var query))
        {
            return query!;
        }

        return context.Request.Query?.ToString() ?? DefaultNoQueryResponse;
    }
    
    private static string GetQuery(IMiddlewareContext context)
    {
        if (TryGetQuery(context, out var query))
        {
            return query!;
        }

        return context.Selection.ToString() ?? DefaultNoQueryResponse;
    }
    
    private static bool TryGetQuery(IHasContextData context, out string? query)
    {
        query = null;
        if (context.ContextData.TryGetValue(HttpContext, out var outContext) &&
            outContext is HttpContext { Request.Body.CanSeek: true } httpContext)
        {
            var requestBody = httpContext.Request.Body;
            requestBody.Seek(0, SeekOrigin.Begin);
            using var reader = new StreamReader(requestBody);
            query = reader.ReadToEndAsync().GetAwaiter().GetResult();
            requestBody.Seek(0, SeekOrigin.Begin);
            return true;
        }

        return false;
    }
}
