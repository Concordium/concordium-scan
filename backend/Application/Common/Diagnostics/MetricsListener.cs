using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using System.Text;

namespace Application.Common.Diagnostics;

public class MetricsListener : IDisposable
{
    private readonly MeterListener _meterListener;
    private readonly ILogger _logger;
    private ConcurrentDictionary<string, ConcurrentBag<long>> _measurements = new();

    public MetricsListener()
    {
        _logger = Log.ForContext(GetType());

        _meterListener = new MeterListener();
        _meterListener.InstrumentPublished = (instrument, listener) =>
        {
            if(instrument.Meter.Name == "CcdScan")
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };
        _meterListener.SetMeasurementEventCallback<long>(OnMeasurementRecorded);
        _meterListener.Start();
    }
    
    private void OnMeasurementRecorded(Instrument instrument, long measurement, ReadOnlySpan<KeyValuePair<string, object?>> tags, object? state)
    {
        var bucket = _measurements.GetOrAdd(instrument.Name, _ => new ConcurrentBag<long>());
        bucket.Add(measurement);
    }

    public void DumpCapturedMetrics()
    {
        var measurements = _measurements;
        _measurements = new();
        
        var rows = measurements
            .OrderBy(x => x.Key)
            .Select(x => new
            {
                Name = x.Key,
                Count = $"{x.Value.Count}",
                Average = $"{x.Value.Average():F1}",
                Total = $"{x.Value.Sum()}",
                Max = $"{x.Value.Max()}",
            })
            .ToList();
        
        rows.Insert(0, new
        {
            Name = "Name",
            Count = "Count",
            Average = "Avg (ms)",
            Total = "Total (ms)",
            Max = "Max (ms)"
        });
        
        var col0Format = $"{{0, -{rows.Max(x => x.Name.Length)}}}";
        var col1Format = $"{{1, {rows.Max(x => x.Count.Length)}}}";
        var col2Format = $"{{2, {rows.Max(x => x.Average.Length)}}}";
        var col3Format = $"{{3, {rows.Max(x => x.Total.Length)}}}";
        var col4Format = $"{{4, {rows.Max(x => x.Max.Length)}}}";
        var rowFormat = $"{Environment.NewLine}   | {col0Format} | {col1Format} | {col2Format} | {col3Format} | {col4Format} |";
        
        var result = new StringBuilder();
        foreach (var row in rows)
            result.AppendFormat(rowFormat, row.Name, row.Count, row.Average, row.Total, row.Max);
        
        _logger.Information($"Captured metrics: {result}");
    }
    
    public void Dispose()
    {
        _meterListener.Dispose();
    }
}