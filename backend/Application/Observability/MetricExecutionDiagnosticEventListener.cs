using System.Text;
using HotChocolate.Execution;
using HotChocolate.Execution.Instrumentation;
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
public sealed class MetricExecutionDiagnosticEventListener : ExecutionDiagnosticEventListener
{
    /// <summary>
    /// Gets the <see cref="StringBuilder"/> pool used by this enricher.
    /// </summary>
    private ObjectPool<StringBuilder> StringBuilderPool { get; }
    
    public MetricExecutionDiagnosticEventListener()
    {
        StringBuilderPool = ObjectPool.Create<StringBuilder>();
    }

    public override IDisposable ExecuteRequest(IRequestContext context)
    {
        var durationMetric = new ApplicationMetrics.GraphQlDurationMetric(context, StringBuilderPool);

        context.ContextData[ApplicationMetrics.GraphQlDurationMetric.GraphQlDurationMetricContextKey] =
            durationMetric;

        return durationMetric;
    }

    public override void RequestError(IRequestContext context, Exception exception)
    {
        if (context.ContextData.TryGetValue(
                ApplicationMetrics.GraphQlDurationMetric.GraphQlDurationMetricContextKey,
                out var metric) &&
            metric is ApplicationMetrics.GraphQlDurationMetric graphQlDurationMetric)
        {
            graphQlDurationMetric.SetException(exception);
        }
    }
}