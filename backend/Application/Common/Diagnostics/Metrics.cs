using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Application.Common.Diagnostics;

public class Metrics : IMetrics, IDisposable
{
    private readonly Meter _meter;
    private readonly ConcurrentDictionary<string, Counter<long>> _durationCounters = new();
    
    public Metrics()
    {
        _meter = new Meter("CcdScan");
    }

    public IDisposable MeasureDuration(string groupingName, string name)
    {
        var counterName = $"{groupingName}-{name}-duration";
        
        if (!_durationCounters.TryGetValue(counterName, out var counter))
        {
            counter = _meter.CreateCounter<long>(counterName);
            _durationCounters[counterName] = counter;
        }

        return new Measurer(counter);
    }

    private class Measurer : IDisposable
    {
        private readonly Counter<long> _counter;
        private readonly Stopwatch _stopWatch;

        public Measurer(Counter<long> counter)
        {
            _counter = counter;
            _stopWatch = Stopwatch.StartNew();
        }

        public void Dispose()
        {
            lock (_counter)
            {
                _counter.Add(_stopWatch.ElapsedMilliseconds);
            }
        }
    }

    public void Dispose()
    {
        _meter.Dispose();
    }
}