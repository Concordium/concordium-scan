using Application.Api.GraphQL;
using Application.Api.GraphQL.Import;

namespace Tests.TestUtilities.Stubs;

public class InitialTokenReleaseScheduleRepositoryStub : IInitialTokenReleaseScheduleRepository
{
    private IOrderedEnumerable<TimestampedAmount> _mainnetSchedule = Array.Empty<TimestampedAmount>().OrderBy(x => x.Timestamp);

    public void SetMainnetSchedule(params TimestampedAmount[] schedule)
    {
        _mainnetSchedule = schedule.OrderBy(x => x.Timestamp);
    }
    
    public IOrderedEnumerable<TimestampedAmount> GetMainnetSchedule()
    {
        return _mainnetSchedule;
    }
}