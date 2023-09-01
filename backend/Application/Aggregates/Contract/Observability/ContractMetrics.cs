using System.Diagnostics;
using Application.Aggregates.Contract.Types;
using Prometheus;
using static Application.Observability.ApplicationMetrics;

namespace Application.Aggregates.Contract.Observability;

internal static class ContractMetrics
{
    private static readonly Gauge SmartReadHeight = Metrics.CreateGauge(
        "contract_read_height",
        "Max height read by any contract import job",
        new GaugeConfiguration
        {
            LabelNames = new []{"data_source"}
        });


    private static readonly Histogram ReadDuration = Metrics.CreateHistogram(
        "contract_read_duration_seconds",
        "Duration of import in seconds",
        new HistogramConfiguration
        {
            LabelNames = new[] { "source", "exception" }
        }
    );

    private static readonly Counter ImportedTransactionEvents = Metrics.CreateCounter(
        "contract_imported_transaction_events_total", "Number of transaction event which has been processed and triggered one or more events",
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
            var elapsedSeconds = _time.ElapsedMilliseconds / 1_000d;
            ReadDuration
                .WithLabels(_source.ToStringCached(), _exceptionName)
                .Observe(elapsedSeconds);
        }
    }
}

