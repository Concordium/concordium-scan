using Serilog.Core;
using Serilog.Events;

namespace Application.Common.Logging;

public class SourceClassNameEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        string result;
        if (logEvent.Properties.TryGetValue("SourceContext", out var sourceContext))
        {
            var sourceContextString = sourceContext.ToString("l", null);
            var classNameStartIndex = sourceContextString.LastIndexOf(".", StringComparison.Ordinal) + 1;

            result = classNameStartIndex > 0
                ? sourceContextString.Substring(classNameStartIndex, sourceContextString.Length - classNameStartIndex)
                : sourceContextString;
        }
        else
            result = "(no context)";
        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("SourceClassName", result));
    }
}