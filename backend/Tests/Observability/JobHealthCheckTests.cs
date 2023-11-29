using Application.Observability;
using FluentAssertions;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Tests.Observability;

public sealed class ContractHealthCheckTests
{
    [Fact]
    public async Task GivenNoFailedJob_WhenCallingHealthCheck_ThenReturnHealthy()
    {
        // Arrange
        var healthCheck = new JobHealthCheck();
        
        // Act
        var checkHealthAsync = await healthCheck.CheckHealthAsync(new HealthCheckContext());
        
        // Assert
        checkHealthAsync.Status.Should().Be(HealthStatus.Healthy);
    }

    [Fact]
    public async Task GivenFailedJob_WhenCallingHealthCheck_ThenReturnUnhealthy()
    {
        // Arrange
        const string key = "foo";
        const string value = "value";
        var healthCheck = new JobHealthCheck();
        healthCheck.AddUnhealthyJobWithMessage(key, value);
        
        // Act
        var checkHealthAsync = await healthCheck.CheckHealthAsync(new HealthCheckContext());
        
        // Assert
        checkHealthAsync.Status.Should().Be(HealthStatus.Degraded);
        checkHealthAsync.Data[key].Should().Be(value);
    }
}
