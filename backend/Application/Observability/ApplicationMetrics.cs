using System.Diagnostics;
using System.Text;
using Application.Exceptions;
using HotChocolate.Execution;
using Microsoft.Extensions.ObjectPool;
using Prometheus;

namespace Application.Observability;

internal static class ApplicationMetrics
{
    private static readonly Histogram GraphQlRequestDuration = Metrics.CreateHistogram(
        "graphql_request_duration_seconds",
        "Duration of GraphQl in seconds",
        new HistogramConfiguration
        {
            LabelNames = new[] { "operation", "exception" }
        });

    private static readonly Counter InteropExceptions = Metrics.CreateCounter(
        "interop_exceptions_total",
        "Number of exceptions from interop calls",
        new CounterConfiguration
        {
            LabelNames = new[] { "method", "error" }
        }
    );

    internal static void IncInteropExceptions(string method, InteropBindingException exception)
    {
        InteropExceptions
            .WithLabels(method, exception.Error.ToStringCached())
            .Inc();
    }
    
    internal class GraphQlDurationMetric : IDisposable
    {
        /// <summary>
        /// This key is used in the GraphQl <see cref="HotChocolate.Execution.IRequestContext"/> in key value
        /// store <see cref="HotChocolate.IHasContextData.ContextData"/>.
        /// </summary>
        internal const string GraphQlDurationMetricContextKey = $"Custom.Diagnostics.{nameof(GraphQlDurationMetric)}";

        private const string DefaultOperationLabel = "GraphQl Request";
        private readonly IRequestContext _context;
        private readonly ObjectPool<StringBuilder> _stringBuilderPool;
        private string _exception = "";
        private readonly Activity _activity;

        public GraphQlDurationMetric(IRequestContext context, ObjectPool<StringBuilder> stringBuilderPool)
        {
            _context = context;
            _stringBuilderPool = stringBuilderPool;
            _activity = TraceContext.StartActivity(nameof(GraphQlDurationMetric));
        }

        internal void SetException(Exception ex)
        {
            _exception = PrettyPrintException(ex);
        }
        
        public void Dispose()
        {
            _activity.Stop();
            var elapsedSeconds = _activity.Duration.TotalSeconds;

            var operation = CreateOperationDisplayName();

            GraphQlRequestDuration
                .WithLabels(operation, _exception)
                .Observe(elapsedSeconds);
            _activity.Dispose();
        }

        private string CreateOperationDisplayName()
        {
            if (_context.Operation is not { } operation) return DefaultOperationLabel;

            var displayName = _stringBuilderPool.Get();

            try
            {
                var rootSelectionSet = operation.RootSelectionSet;

                displayName.Append('{');
                displayName.Append(' ');

                foreach (var selection in rootSelectionSet.Selections.Take(3))
                {
                    if (displayName.Length > 2)
                    {
                        displayName.Append(' ');
                    }

                    displayName.Append(selection.ResponseName);
                }

                if (rootSelectionSet.Selections.Count > 3)
                {
                    displayName.Append(' ');
                    displayName.Append('.');
                    displayName.Append('.');
                    displayName.Append('.');
                }

                displayName.Append(' ');
                displayName.Append('}');

                if (operation.Name is { } name)
                {
                    displayName.Insert(0, ' ');
                    displayName.Insert(0, name);
                }

                displayName.Insert(0, ' ');
                displayName.Insert(0, operation.Definition.Operation.ToString().ToLowerInvariant());

                return displayName.ToString();
            }
            finally
            {
                _stringBuilderPool.Return(displayName.Clear());
            }
        }
    }
    
    internal static string PrettyPrintException(Exception ex)
    {
        var type = ex.GetType();
        if (type.GenericTypeArguments.Length == 0)
        {
            return type.Name;
        }

        var name = type.Name.AsSpan();
        var indexOfGenericCount = name.IndexOf('`');
        if (indexOfGenericCount != -1)
        {
            name = name[..indexOfGenericCount];
        }
        var typeArguments = string.Join(",", type.GenericTypeArguments.Select(t => t.Name));

        return $"{name}<{typeArguments}>";
    }
}
