using Application.Common;

namespace Tests.TestUtilities.Stubs;

public class TimeProviderStub : ITimeProvider
{
    public TimeProviderStub() : this(new DateTimeOffset(2020, 10, 1, 12, 44, 31, 0, TimeSpan.Zero))
    {
    }

    public TimeProviderStub(DateTimeOffset utcNow)
    {
        UtcNow = utcNow;
    }

    public DateTimeOffset UtcNow { get; set; }
}