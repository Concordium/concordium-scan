using System.Threading;
using Application.Aggregates.Contract.Jobs;

namespace Tests.Aggregates.Contract.Jobs;

public sealed class ParallelBatchBlockHeightJobTests
{
    public 
}

internal sealed class MockStatelessBlockHeightJobs : IStatelessBlockHeightJobs
{
    public string GetUniqueIdentifier()
    {
        throw new NotImplementedException();
    }

    public Task<long> GetFinalHeight(CancellationToken token)
    {
        throw new NotImplementedException();
    }

    public Task UpdateMetric(CancellationToken token)
    {
        throw new NotImplementedException();
    }

    public Task<ulong> BatchImportJob(ulong heightFrom, ulong heightTo, CancellationToken token = default)
    {
        throw new NotImplementedException();
    }
}
