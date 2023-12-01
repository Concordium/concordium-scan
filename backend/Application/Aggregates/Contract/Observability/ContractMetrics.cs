using Application.Aggregates.Contract.Types;
using Application.Observability;
using Prometheus;

namespace Application.Aggregates.Contract.Observability;

internal static class ContractMetrics
{
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
        ApplicationMetrics.SetReadHeight(value, "contract", source);
    }

    internal static ApplicationMetrics.DurationMetric CreateContractReadDurationMetric(ImportSource source) => 
        new("contract_import", source);
}

