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
                new TimestampedAmount(new DateTimeOffset(2022, 01, 05, 0, 0, 0, TimeSpan.Zero), 10_335_388_948_000_000),
                new TimestampedAmount(new DateTimeOffset(2022, 02, 05, 0, 0, 0, TimeSpan.Zero), 9_854_804_226_000_000),
                new TimestampedAmount(new DateTimeOffset(2022, 03, 05, 0, 0, 0, TimeSpan.Zero), 9_506_286_976_000_000),
                new TimestampedAmount(new DateTimeOffset(2022, 04, 05, 0, 0, 0, TimeSpan.Zero), 9_157_769_725_000_000),
                new TimestampedAmount(new DateTimeOffset(2022, 05, 05, 0, 0, 0, TimeSpan.Zero), 8_809_252_474_000_000),
                new TimestampedAmount(new DateTimeOffset(2022, 06, 05, 0, 0, 0, TimeSpan.Zero), 8_460_735_224_000_000),
                new TimestampedAmount(new DateTimeOffset(2022, 07, 05, 0, 0, 0, TimeSpan.Zero), 7_827_764_959_000_000),
                new TimestampedAmount(new DateTimeOffset(2022, 08, 05, 0, 0, 0, TimeSpan.Zero), 7_194_794_695_000_000),
                new TimestampedAmount(new DateTimeOffset(2022, 09, 05, 0, 0, 0, TimeSpan.Zero), 6_561_824_431_000_000),
                new TimestampedAmount(new DateTimeOffset(2022, 10, 05, 0, 0, 0, TimeSpan.Zero), 5_928_854_166_000_000),
                new TimestampedAmount(new DateTimeOffset(2022, 11, 05, 0, 0, 0, TimeSpan.Zero), 5_625_324_740_000_000),
                new TimestampedAmount(new DateTimeOffset(2022, 12, 05, 0, 0, 0, TimeSpan.Zero), 5_321_795_314_000_000),
                new TimestampedAmount(new DateTimeOffset(2023, 01, 05, 0, 0, 0, TimeSpan.Zero), 5_018_265_888_000_000),
                new TimestampedAmount(new DateTimeOffset(2023, 02, 05, 0, 0, 0, TimeSpan.Zero), 4_714_736_461_000_000),
                new TimestampedAmount(new DateTimeOffset(2023, 03, 05, 0, 0, 0, TimeSpan.Zero), 4_411_207_035_000_000),
                new TimestampedAmount(new DateTimeOffset(2023, 04, 05, 0, 0, 0, TimeSpan.Zero), 4_107_677_609_000_000),
                new TimestampedAmount(new DateTimeOffset(2023, 05, 05, 0, 0, 0, TimeSpan.Zero), 3_804_148_183_000_000),
                new TimestampedAmount(new DateTimeOffset(2023, 06, 05, 0, 0, 0, TimeSpan.Zero), 3_500_618_756_000_000),
                new TimestampedAmount(new DateTimeOffset(2023, 07, 05, 0, 0, 0, TimeSpan.Zero), 3_082_940_774_000_000),
                new TimestampedAmount(new DateTimeOffset(2023, 08, 05, 0, 0, 0, TimeSpan.Zero), 2_949_715_805_000_000),
                new TimestampedAmount(new DateTimeOffset(2023, 09, 05, 0, 0, 0, TimeSpan.Zero), 2_816_490_837_000_000),
                new TimestampedAmount(new DateTimeOffset(2023, 10, 05, 0, 0, 0, TimeSpan.Zero), 2_683_265_868_000_000),
                new TimestampedAmount(new DateTimeOffset(2023, 11, 05, 0, 0, 0, TimeSpan.Zero), 2_550_040_899_000_000),
                new TimestampedAmount(new DateTimeOffset(2023, 12, 05, 0, 0, 0, TimeSpan.Zero), 2_416_815_931_000_000),
                new TimestampedAmount(new DateTimeOffset(2024, 01, 05, 0, 0, 0, TimeSpan.Zero), 2_283_590_962_000_000),
                new TimestampedAmount(new DateTimeOffset(2024, 02, 05, 0, 0, 0, TimeSpan.Zero), 2_150_365_993_000_000),
                new TimestampedAmount(new DateTimeOffset(2024, 03, 05, 0, 0, 0, TimeSpan.Zero), 2_017_141_024_000_000),
                new TimestampedAmount(new DateTimeOffset(2024, 04, 05, 0, 0, 0, TimeSpan.Zero), 1_883_916_056_000_000),
                new TimestampedAmount(new DateTimeOffset(2024, 05, 05, 0, 0, 0, TimeSpan.Zero), 1_750_691_087_000_000),
                new TimestampedAmount(new DateTimeOffset(2024, 06, 05, 0, 0, 0, TimeSpan.Zero), 1_617_466_118_000_000),
                new TimestampedAmount(new DateTimeOffset(2024, 07, 05, 0, 0, 0, TimeSpan.Zero), 1_484_241_150_000_000),
                new TimestampedAmount(new DateTimeOffset(2024, 08, 05, 0, 0, 0, TimeSpan.Zero), 1_351_016_181_000_000),
                new TimestampedAmount(new DateTimeOffset(2024, 09, 05, 0, 0, 0, TimeSpan.Zero), 1_217_791_212_000_000),
                new TimestampedAmount(new DateTimeOffset(2024, 10, 05, 0, 0, 0, TimeSpan.Zero), 1_084_566_244_000_000),
                new TimestampedAmount(new DateTimeOffset(2024, 11, 05, 0, 0, 0, TimeSpan.Zero), 951_341_275_000_000),
                new TimestampedAmount(new DateTimeOffset(2024, 12, 05, 0, 0, 0, TimeSpan.Zero), 818_116_306_000_000),
                new TimestampedAmount(new DateTimeOffset(2025, 01, 05, 0, 0, 0, TimeSpan.Zero), 684_891_338_000_000),
                new TimestampedAmount(new DateTimeOffset(2025, 02, 05, 0, 0, 0, TimeSpan.Zero), 570_742_781_000_000),
                new TimestampedAmount(new DateTimeOffset(2025, 03, 05, 0, 0, 0, TimeSpan.Zero), 456_594_225_000_000),
                new TimestampedAmount(new DateTimeOffset(2025, 04, 05, 0, 0, 0, TimeSpan.Zero), 342_445_669_000_000),
                new TimestampedAmount(new DateTimeOffset(2025, 05, 05, 0, 0, 0, TimeSpan.Zero), 228_297_113_000_000),
                new TimestampedAmount(new DateTimeOffset(2025, 06, 05, 0, 0, 0, TimeSpan.Zero), 114_148_556_000_000),
                new TimestampedAmount(new DateTimeOffset(2025, 07, 05, 0, 0, 0, TimeSpan.Zero), 0),
            }
            .OrderBy(x => x.Timestamp);
    }

    public IOrderedEnumerable<TimestampedAmount> GetMainnetSchedule()
    {
        return _mainnetSchedule;
    }
}