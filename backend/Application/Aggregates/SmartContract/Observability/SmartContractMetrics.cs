using System.Diagnostics;
using Application.Aggregates.SmartContract.Types;
using Prometheus;

namespace Application.Aggregates.SmartContract.Observability;

internal static class SmartContractMetrics
{
    private static readonly Gauge SmartReadHeight = Metrics.CreateGauge(
        "smart_contract_read_height",
        "Max height read by any smart contract import job",
        new GaugeConfiguration
        {
            LabelNames = new []{"data_source"}
        });


    private static readonly Histogram ReadDuration = Metrics.CreateHistogram(
        "smart_contract_read_duration_seconds",
        "Duration of import in seconds",
        new HistogramConfiguration
        {
            LabelNames = new[] { "source", "exception" }
        }
    );

    private static readonly Counter ImportedTransactionEvents = Metrics.CreateCounter(
        "smart_contract_imported_transaction_events_total", "Number of transaction event which has been processed and triggered one or more events",
        new CounterConfiguration
        {
            LabelNames = new[] { "source" }
        }
    );

    internal static void IncTransactionEvents(double count, ImportSource source)
    {
        ImportedTransactionEvents
            .WithLabels(source.ToStringCached())
            .Inc(count);
    }

    internal static void SetReadHeight(double value, ImportSource source)
    {
        SmartReadHeight
            .WithLabels(source.ToStringCached())
            .Set(value);
    }
    
    internal class DurationMetric : IDisposable
    {
        private string _exceptionName = "";
        private readonly Stopwatch _time;
        private readonly ImportSource _source;
    
        public DurationMetric(ImportSource source)
        {
            _source = source;
            _time = Stopwatch.StartNew();
        }

        internal void SetException(Exception ex)
        {
            _exceptionName = PrettyPrintException(ex);
        }

        public void Dispose()
        {
            ReadDuration
                .WithLabels(_source.ToStringCached(), _exceptionName)
                .Observe(_time.ElapsedMilliseconds / 1_000);
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
}

