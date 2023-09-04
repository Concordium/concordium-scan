using System.Diagnostics;
using Serilog.Core;
using Serilog.Events;

namespace Application.Common.Logging;

public class TraceEnricher : ILogEventEnricher
{
    private const string TraceId = "TraceId";
    
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        if (Activity.Current is null || Activity.Current.Id is null)
        {
            return;
        }
        
        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(TraceId, Activity.Current.Id));
    }
}