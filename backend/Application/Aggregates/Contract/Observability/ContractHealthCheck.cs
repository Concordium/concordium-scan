using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Application.Aggregates.Contract.Observability;

public sealed class ContractHealthCheck : IHealthCheck
{
    private readonly ConcurrentDictionary<string, string> _unhealthyJobs = new();

    public void AddUnhealthyJobWithMessage(string job, string message)
    {
        _unhealthyJobs.AddOrUpdate(job, message, (_, _) => message);
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = new CancellationToken())
    {
        if (_unhealthyJobs.IsEmpty)
        {
            return Task.FromResult(HealthCheckResult.Healthy());
        }
        
        return Task.FromResult(HealthCheckResult.Degraded("Some jobs have degraded",
            data: _unhealthyJobs.ToDictionary(kvp => kvp.Key, kvp => (object)kvp.Value)));
    }
}