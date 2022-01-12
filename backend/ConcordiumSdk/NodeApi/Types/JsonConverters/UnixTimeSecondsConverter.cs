using System.Text.Json;
using System.Text.Json.Serialization;

namespace ConcordiumSdk.NodeApi.Types.JsonConverters;

public class UnixTimeSecondsConverter : JsonConverter<UnixTimeSeconds>
{
    public override UnixTimeSeconds Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        EnsureTokenType(reader, JsonTokenType.Number);
        return new UnixTimeSeconds(reader.GetInt64());
    }

    public override void Write(Utf8JsonWriter writer, UnixTimeSeconds value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value.AsLong);
    }
    
    private static void EnsureTokenType(Utf8JsonReader reader, JsonTokenType tokenType)
    {
        if (reader.TokenType != tokenType)
            throw new JsonException($"Must be {tokenType}.");
    }
}

public class UnixTimeSeconds
{
    public UnixTimeSeconds(long value)
    {
        AsLong = value;
    }

    public long AsLong { get; }

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