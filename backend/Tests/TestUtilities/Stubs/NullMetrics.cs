using Application.Common.Diagnostics;

namespace Tests.TestUtilities.Stubs;

public class NullMetrics : IMetrics
{
    public IDisposable MeasureDuration(string groupingName, string name)
    {
        return new NullDisposable();
    }

    private class NullDisposable : IDisposable
    {
        public void Dispose()
        {
        }
    }
}