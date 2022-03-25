using HotChocolate.Types;

namespace Application.Api.GraphQL;

public class AccountReleaseSchedule
{
    [UsePaging(InferConnectionNameFromField = false)]
    public AccountReleaseScheduleItem[] Schedule { get; }

    public ulong TotalAmount { get; }

    public AccountReleaseSchedule(AccountReleaseScheduleItem[] schedule)
    {
        Schedule = schedule;
        TotalAmount = schedule.Aggregate(0UL, (val, item) => val + item.Amount);
    }
}