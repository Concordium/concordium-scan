using System.Threading.Tasks;
using Application.Observability;
using Grpc.Core;
using Npgsql;
using Polly;

namespace Application.Resilience;

internal static class Policies
{
    /// <summary>
    /// Create an async retry policy, which retries on all transient database errors.
    /// </summary>
    internal static AsyncPolicy GetTransientPolicy(string process, ILogger logger, int retryCount, TimeSpan delay)
    {
        var policyBuilder = Policy
            .Handle<NpgsqlException>(ex => ex.IsTransient)
            .OrInner<NpgsqlException>(ex => ex.IsTransient)
            .Or<RpcException>(ex => ex.StatusCode == StatusCode.Unavailable);
        AsyncPolicy policy;
        if (retryCount == -1)
        {
            policy = policyBuilder
                .WaitAndRetryForeverAsync((_, _, _) => delay,
                    (ex, currentRetryCount, _, _) =>
                    {
                        logger.Error(ex, $"Triggering retry policy with {currentRetryCount} due to exception");
                        ApplicationMetrics.IncRetryPolicyExceptions(process, ex);
                        return Task.CompletedTask;
                    });
        }
        else
        {
            policy = policyBuilder
                .WaitAndRetryAsync(retryCount,
                    (_, _, _) => delay,
                    (ex, _, currentRetryCount, _) =>
                    {
                        logger.Error(ex, $"Triggering retry policy with {currentRetryCount} due to exception");
                        ApplicationMetrics.IncRetryPolicyExceptions(process, ex);
                        return Task.CompletedTask;
                    });
        }

        return policy;
    }    
    
    /// <summary>
    /// Create an async retry policy, which retries on all transient database errors.
    /// </summary>
    internal static AsyncPolicy<T> GetTransientPolicy<T>(string process, ILogger logger, int retryCount, TimeSpan delay)
    {
        var policyBuilder = Policy<T>
            .Handle<NpgsqlException>(ex => ex.IsTransient)
            .OrInner<NpgsqlException>(ex => ex.IsTransient);
        AsyncPolicy<T> policy;
        if (retryCount == -1)
        {
            policy = policyBuilder
                .WaitAndRetryForeverAsync((_, _, _) => delay,
                    (ex, currentRetryCount, _, _) =>
                    {
                        logger.Error(ex.Exception, $"Triggering retry policy with {currentRetryCount} due to exception");
                        ApplicationMetrics.IncRetryPolicyExceptions(process, ex.Exception);
                        return Task.CompletedTask;
                    });
        }
        else
        {
            policy = policyBuilder
                .WaitAndRetryAsync(retryCount,
                    (_, _, _) => delay,
                    (ex, _, currentRetryCount, _) =>
                    {
                        logger.Error(ex.Exception, $"Triggering retry policy with {currentRetryCount} due to exception");
                        ApplicationMetrics.IncRetryPolicyExceptions(process, ex.Exception);
                        return Task.CompletedTask;
                    });
        }

        return policy;
    }    
}
