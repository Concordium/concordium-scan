namespace Application.Common.Diagnostics;

public interface IMetrics
{
    IDisposable MeasureDuration(string groupingName, string name);
}