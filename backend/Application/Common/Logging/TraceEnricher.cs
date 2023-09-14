using System.Diagnostics;
using Serilog.Core;
using Serilog.Events;

namespace Application.Common.Logging;

public class TraceEnricher : ILogEventEnricher
{
    private const string Id = "Id";
    private const string TraceId = "TraceId";
    private const string SpanId = "SpanId";
    
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        if (Activity.Current is null || Activity.Current.Id is null)
        {
            return;
        }
        
        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(Id, Activity.Current.Id));
        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(TraceId, Activity.Current.TraceId));
        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(SpanId, Activity.Current.SpanId));
    }
}
