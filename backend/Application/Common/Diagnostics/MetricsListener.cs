using System.Diagnostics.Metrics;
using System.Text;

namespace Application.Common.Diagnostics;

public class MetricsListener : IDisposable
{
    private readonly MeterListener _meterListener;
    private readonly ILogger _logger;
    private readonly Dictionary<string, List<long>> _measurements = new();

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
        if (!_measurements.TryGetValue(instrument.Name, out var bucket))
        {
            bucket = new List<long>();
            _measurements[instrument.Name] = bucket;
        }
        bucket.Add(measurement);
    }

    public void DumpCapturedMetrics()
    {
        var result = new StringBuilder();
        foreach (var measurement in _measurements.OrderBy(x => x.Key))
            result.Append($"{Environment.NewLine}   {measurement.Key} [count:{measurement.Value.Count}] [average:{measurement.Value.Average():F0}ms] [max:{measurement.Value.Max()}ms]");
        
        _logger.Information($"Captured metrics: {result}");
        _measurements.Clear();
    }
    
    public void Dispose()
    {
        _meterListener.Dispose();
    }
}