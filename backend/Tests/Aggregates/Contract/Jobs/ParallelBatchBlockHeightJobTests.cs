using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Application.Aggregates.Contract.Configurations;
using Application.Aggregates.Contract.Jobs;
using Application.Configurations;
using FluentAssertions;
using Microsoft.Extensions.Options;

namespace Tests.Aggregates.Contract.Jobs;

public sealed class ParallelBatchBlockHeightJobTests
{
    [Fact]
    public async Task WhenImportBatch_ThenProcessAllHeightOnce()
    {
        // Arrange
        var statelessJob = new MockStatelessBlockHeightJobs();
        var options = Options.Create(new ContractAggregateOptions
        {
            Jobs = new Dictionary<string, JobOptions>
            {
                { statelessJob.GetUniqueIdentifier(), new JobOptions
                {
                    BatchSize = 5
                }}
            }
        });
        var parallelBatchBlockHeightJob = new ParallelBatchBlockHeightJob<MockStatelessBlockHeightJobs>(
            statelessJob, options
            );
        
        // Act
        await parallelBatchBlockHeightJob.StartImport(CancellationToken.None);
        
        // Assert
        statelessJob.Storage.Keys.Count.Should().Be((int)MockStatelessBlockHeightJobs.FinalHeightToRead + 1);
        statelessJob.Storage.Values.Count(v => v != 1).Should().Be(0);
    }
}

internal sealed class MockStatelessBlockHeightJobs : IStatelessBlockHeightJobs
{
    private int _finalHeightCalledCount;
    internal const long FinalHeightToRead = 2_000L;
    internal readonly ConcurrentDictionary<ulong, int> Storage = new();
    
    public string GetUniqueIdentifier() => nameof(MockStatelessBlockHeightJobs);

    public Task<long> GetMaximumHeight(CancellationToken token)
    {
        _finalHeightCalledCount += 1;
        var toReturn = _finalHeightCalledCount switch
        {
            1 => 1_000L,
            _ => FinalHeightToRead,
        };
        return Task.FromResult(toReturn);
    }

    public Task UpdateMetric(CancellationToken token)
    {
        return Task.CompletedTask;
    }
    
    public Task<ulong> BatchImportJob(ulong heightFrom, ulong heightTo, CancellationToken token = default)
    {
        var affected = 0UL; 
        for (var height = heightFrom; height <= heightTo; height++)
        {
            Storage.AddOrUpdate(height, _ => 1, (_, old) => old + 1);
            affected += 1;
        }

        return Task.FromResult(affected);
    }

    public bool ShouldNodeImportAwait()
    {
        throw new NotImplementedException();
    }
}
