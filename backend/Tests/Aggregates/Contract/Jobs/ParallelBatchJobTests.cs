using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using Application.Aggregates.Contract.Configurations;
using Application.Aggregates.Contract.Jobs;
using Application.Aggregates.Contract.Observability;
using FluentAssertions;
using Microsoft.Extensions.Options;

namespace Tests.Aggregates.Contract.Jobs;

public sealed class ParallelBatchJobTests
{

    [Fact]
    public async Task WhenImport_ThenAllProcessed()
    {
        // Arrange
        var expected = Enumerable.Range(0,42).ToImmutableSortedSet();
        var statelessMockJob = new StatelessMockJob();
        var parallelBatchJob = new ParallelBatchJob<StatelessMockJob>(
            statelessMockJob,
            Options.Create(new ContractAggregateOptions()),
            new ContractHealthCheck()
            );
        
        // Act
        await parallelBatchJob.StartImport(CancellationToken.None);
        
        // Assert
        statelessMockJob.GetState()
            .Should()
            .BeEquivalentTo(expected);
    }
    
    private sealed class StatelessMockJob : IStatelessJob
    {
        private readonly ConcurrentBag<int> _state = new();

        public ImmutableSortedSet<int> GetState() => _state.ToImmutableSortedSet();

        public string GetUniqueIdentifier() => nameof(StatelessMockJob);

        public Task<IEnumerable<int>> GetBatches(CancellationToken cancellationToken)
        {
            return Task.FromResult(Enumerable.Range(0, 42));
        }

        public ValueTask Process(int batch, CancellationToken token = default)
        {
            _state.Add(batch);
            return ValueTask.CompletedTask;
        }

        public bool ShouldNodeImportAwait() => false;
    }
    
}

