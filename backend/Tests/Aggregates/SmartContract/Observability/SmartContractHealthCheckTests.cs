using Application.Aggregates.SmartContract.Observability;
using FluentAssertions;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Tests.Aggregates.SmartContract.Observability;

public sealed class SmartContractHealthCheckTests
{
    [Fact]
    public async Task GivenNoFailedJob_WhenCallingHealthCheck_ThenReturnHealthy()
    {
        // Arrange
        var healthCheck = new SmartContractHealthCheck();
        
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
        var healthCheck = new SmartContractHealthCheck();
        healthCheck.AddUnhealthyJobWithMessage(key, value);
        
        // Act
        var checkHealthAsync = await healthCheck.CheckHealthAsync(new HealthCheckContext());
        
        // Assert
        checkHealthAsync.Status.Should().Be(HealthStatus.Unhealthy);
        checkHealthAsync.Data[key].Should().Be(value);
    }
}