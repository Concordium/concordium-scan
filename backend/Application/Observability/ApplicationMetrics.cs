using System.Diagnostics;
using System.Text;
using Application.Aggregates.Contract.Types;
using Application.Exceptions;
using Concordium.Sdk.Types;
using HotChocolate.Execution;
using Microsoft.Extensions.ObjectPool;
using Prometheus;

namespace Application.Observability;

internal static class ApplicationMetrics
{
    private static readonly Histogram ProcessDuration = Metrics.CreateHistogram(
        "process_duration_seconds",
        "Duration of a process in seconds",
        new HistogramConfiguration
        {
            LabelNames = new[] { "process", "source", "exception" }
        }
    );
    
    private static readonly Gauge ProcessReadHeight = Metrics.CreateGauge(
        "import_process_read_height",
        "Max height read by an import process",
        new GaugeConfiguration
        {
            LabelNames = new []{"process", "data_source"}
        });
    
    private static readonly Histogram GraphQlRequestDuration = Metrics.CreateHistogram(
        "graphql_request_duration_seconds",
        "Duration of GraphQl in seconds",
        new HistogramConfiguration
        {
            LabelNames = new[] { "operation", "exception" }
        });

    private static readonly Counter InteropErrors = Metrics.CreateCounter(
        "interop_errors_total",
        "Number of errors from interop calls. Instigator is used to partition which process initiated the call.",
        new CounterConfiguration
        {
            LabelNames = new[] { "instigator", "exception" }
        }
    );

    private static readonly Counter RetryPolicyExceptions = Metrics.CreateCounter(
        "retry_policy_exceptions_total",
        "Number of retry policies triggered with the exception triggering it",
        new CounterConfiguration
        {
            LabelNames = new[] { "process", "exception" }
        }
    );

    private static readonly Counter TotalAccountCreated = Metrics.CreateCounter(
        "accounts_created_total",
        "Total number of accounts created");

    private static void AddProcessDuration(TimeSpan elapsed, string process, ImportSource source, Exception? exception)
    {
        var exceptionName = exception != null ? PrettyPrintException(exception) : "";
        var elapsedSeconds = elapsed.TotalMilliseconds / 1_000d;
        ProcessDuration
            .WithLabels(process, source.ToStringCached(), exceptionName)
            .Observe(elapsedSeconds);
    }

    internal static void IncAccountCreated(int accountsCreated)
    {
        TotalAccountCreated
            .Inc(accountsCreated);
    }
    
    internal static void SetReadHeight(double value, string processIdentifier, ImportSource source)
    {
        ProcessReadHeight
            .WithLabels(processIdentifier, source.ToStringCached())
            .Set(value);
    }

    internal static void IncInteropErrors(string instigator, InteropBindingException exception)
    {
        InteropErrors
            .WithLabels(instigator, exception.Error.ToStringCached())
            .Inc();
    }

    internal static void IncRetryPolicyExceptions(string process, Exception exception)
    {
        RetryPolicyExceptions
            .WithLabels(process, PrettyPrintException(exception))
            .Inc();
    }
    
    internal class DurationMetric : IDisposable
    {
        private Exception? _exception;
        private readonly Stopwatch _time;
        private readonly ImportSource _source;
        private readonly string _process;

        public DurationMetric(string process, ImportSource source)
        {
            _process = process;
            _source = source;
            _time = Stopwatch.StartNew();
        }

        internal void SetException(Exception ex)
        {
            _exception = ex;
        }

        public void Dispose()
        {
            AddProcessDuration(_time.Elapsed, _process, _source, _exception);
        }
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

    private static string PrettyPrintException(Exception ex)
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
