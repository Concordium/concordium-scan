namespace Application.Api.GraphQL.Import;

public interface IInitialTokenReleaseScheduleRepository
{
    IOrderedEnumerable<TimestampedAmount> GetMainnetSchedule();
}

public class InitialTokenReleaseScheduleRepository : IInitialTokenReleaseScheduleRepository
{
    private readonly IOrderedEnumerable<TimestampedAmount> _mainnetSchedule;

    public InitialTokenReleaseScheduleRepository()
    {
        _mainnetSchedule = CreateMainnetSchedule();
    }

    private IOrderedEnumerable<TimestampedAmount> CreateMainnetSchedule()
    {
        return new[]
            {
                new TimestampedAmount(new DateTimeOffset(2022, 01, 05, 0, 0, 0, TimeSpan.Zero), 10335388948),
                new TimestampedAmount(new DateTimeOffset(2022, 02, 05, 0, 0, 0, TimeSpan.Zero), 9854804226),
                new TimestampedAmount(new DateTimeOffset(2022, 03, 05, 0, 0, 0, TimeSpan.Zero), 9506286976),
                new TimestampedAmount(new DateTimeOffset(2022, 04, 05, 0, 0, 0, TimeSpan.Zero), 9157769725),
                new TimestampedAmount(new DateTimeOffset(2022, 05, 05, 0, 0, 0, TimeSpan.Zero), 8809252474),
                new TimestampedAmount(new DateTimeOffset(2022, 06, 05, 0, 0, 0, TimeSpan.Zero), 8460735224),
                new TimestampedAmount(new DateTimeOffset(2022, 07, 05, 0, 0, 0, TimeSpan.Zero), 7827764959),
                new TimestampedAmount(new DateTimeOffset(2022, 08, 05, 0, 0, 0, TimeSpan.Zero), 7194794695),
                new TimestampedAmount(new DateTimeOffset(2022, 09, 05, 0, 0, 0, TimeSpan.Zero), 6561824431),
                new TimestampedAmount(new DateTimeOffset(2022, 10, 05, 0, 0, 0, TimeSpan.Zero), 5928854166),
                new TimestampedAmount(new DateTimeOffset(2022, 11, 05, 0, 0, 0, TimeSpan.Zero), 5625324740),
                new TimestampedAmount(new DateTimeOffset(2022, 12, 05, 0, 0, 0, TimeSpan.Zero), 5321795314),
                new TimestampedAmount(new DateTimeOffset(2023, 01, 05, 0, 0, 0, TimeSpan.Zero), 5018265888),
                new TimestampedAmount(new DateTimeOffset(2023, 02, 05, 0, 0, 0, TimeSpan.Zero), 4714736461),
                new TimestampedAmount(new DateTimeOffset(2023, 03, 05, 0, 0, 0, TimeSpan.Zero), 4411207035),
                new TimestampedAmount(new DateTimeOffset(2023, 04, 05, 0, 0, 0, TimeSpan.Zero), 4107677609),
                new TimestampedAmount(new DateTimeOffset(2023, 05, 05, 0, 0, 0, TimeSpan.Zero), 3804148183),
                new TimestampedAmount(new DateTimeOffset(2023, 06, 05, 0, 0, 0, TimeSpan.Zero), 3500618756),
                new TimestampedAmount(new DateTimeOffset(2023, 07, 05, 0, 0, 0, TimeSpan.Zero), 3082940774),
                new TimestampedAmount(new DateTimeOffset(2023, 08, 05, 0, 0, 0, TimeSpan.Zero), 2949715805),
                new TimestampedAmount(new DateTimeOffset(2023, 09, 05, 0, 0, 0, TimeSpan.Zero), 2816490837),
                new TimestampedAmount(new DateTimeOffset(2023, 10, 05, 0, 0, 0, TimeSpan.Zero), 2683265868),
                new TimestampedAmount(new DateTimeOffset(2023, 11, 05, 0, 0, 0, TimeSpan.Zero), 2550040899),
                new TimestampedAmount(new DateTimeOffset(2023, 12, 05, 0, 0, 0, TimeSpan.Zero), 2416815931),
                new TimestampedAmount(new DateTimeOffset(2024, 01, 05, 0, 0, 0, TimeSpan.Zero), 2283590962),
                new TimestampedAmount(new DateTimeOffset(2024, 02, 05, 0, 0, 0, TimeSpan.Zero), 2150365993),
                new TimestampedAmount(new DateTimeOffset(2024, 03, 05, 0, 0, 0, TimeSpan.Zero), 2017141024),
                new TimestampedAmount(new DateTimeOffset(2024, 04, 05, 0, 0, 0, TimeSpan.Zero), 1883916056),
                new TimestampedAmount(new DateTimeOffset(2024, 05, 05, 0, 0, 0, TimeSpan.Zero), 1750691087),
                new TimestampedAmount(new DateTimeOffset(2024, 06, 05, 0, 0, 0, TimeSpan.Zero), 1617466118),
                new TimestampedAmount(new DateTimeOffset(2024, 07, 05, 0, 0, 0, TimeSpan.Zero), 1484241150),
                new TimestampedAmount(new DateTimeOffset(2024, 08, 05, 0, 0, 0, TimeSpan.Zero), 1351016181),
                new TimestampedAmount(new DateTimeOffset(2024, 09, 05, 0, 0, 0, TimeSpan.Zero), 1217791212),
                new TimestampedAmount(new DateTimeOffset(2024, 10, 05, 0, 0, 0, TimeSpan.Zero), 1084566244),
                new TimestampedAmount(new DateTimeOffset(2024, 11, 05, 0, 0, 0, TimeSpan.Zero), 951341275),
                new TimestampedAmount(new DateTimeOffset(2024, 12, 05, 0, 0, 0, TimeSpan.Zero), 818116306),
                new TimestampedAmount(new DateTimeOffset(2025, 01, 05, 0, 0, 0, TimeSpan.Zero), 684891338),
                new TimestampedAmount(new DateTimeOffset(2025, 02, 05, 0, 0, 0, TimeSpan.Zero), 570742781),
                new TimestampedAmount(new DateTimeOffset(2025, 03, 05, 0, 0, 0, TimeSpan.Zero), 456594225),
                new TimestampedAmount(new DateTimeOffset(2025, 04, 05, 0, 0, 0, TimeSpan.Zero), 342445669),
                new TimestampedAmount(new DateTimeOffset(2025, 05, 05, 0, 0, 0, TimeSpan.Zero), 228297113),
                new TimestampedAmount(new DateTimeOffset(2025, 06, 05, 0, 0, 0, TimeSpan.Zero), 114148556),
                new TimestampedAmount(new DateTimeOffset(2025, 07, 05, 0, 0, 0, TimeSpan.Zero), 0),
            }
            .OrderBy(x => x.Timestamp);
    }

    public IOrderedEnumerable<TimestampedAmount> GetMainnetSchedule()
    {
        return _mainnetSchedule;
    }
}