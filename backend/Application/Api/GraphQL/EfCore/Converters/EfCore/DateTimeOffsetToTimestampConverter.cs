using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Application.Api.GraphQL.EfCore.Converters.EfCore;

public class DateTimeOffsetToTimestampConverter : ValueConverter<DateTimeOffset, DateTime>
{
    public DateTimeOffsetToTimestampConverter() : base(
    v => ConvertToDateTime(v),
    v => ConvertToDateTimeOffset(v))
    {
    }

    private static DateTimeOffset ConvertToDateTimeOffset(DateTime value)
    {
        return DateTime.SpecifyKind(value, DateTimeKind.Utc);
    }

    private static DateTime ConvertToDateTime(DateTimeOffset value)
    {
        if (value.Offset != TimeSpan.Zero) throw new ArgumentException("Can only convert with offset zero!");
        return DateTime.SpecifyKind(value.DateTime, DateTimeKind.Utc);
    }
}