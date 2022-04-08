namespace Application.Common;

public static class DateTimeOffsetExtensions
{
    public static DateTimeOffset AsUtcDateTimeOffset(this DateTime value)
    {
        return DateTime.SpecifyKind(value, DateTimeKind.Utc);
    }
   
}