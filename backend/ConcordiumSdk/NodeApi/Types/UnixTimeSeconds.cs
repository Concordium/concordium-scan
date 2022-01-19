namespace ConcordiumSdk.NodeApi.Types;

public class UnixTimeSeconds
{
    public UnixTimeSeconds(long value)
    {
        AsLong = value;
    }

    public long AsLong { get; }
    public DateTimeOffset AsDateTimeOffset => DateTimeOffset.FromUnixTimeSeconds(AsLong);

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        var other = (UnixTimeSeconds)obj;
        return AsLong == other.AsLong;
    }

    public override int GetHashCode()
    {
        return AsLong.GetHashCode();
    }

    public static bool operator ==(UnixTimeSeconds? left, UnixTimeSeconds? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(UnixTimeSeconds? left, UnixTimeSeconds? right)
    {
        return !Equals(left, right);
    }
}