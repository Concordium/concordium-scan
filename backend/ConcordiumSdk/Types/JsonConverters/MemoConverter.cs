using System.Text.Json;
using System.Text.Json.Serialization;

namespace ConcordiumSdk.Types.JsonConverters;

public class MemoConverter : JsonConverter<Memo>
{
    public override Memo Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        if (value == null)
            throw new JsonException("Memo cannot be null.");
        return Memo.FromHexString(value);
    }

    public override void Write(Utf8JsonWriter writer, Memo value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.AsHex);
    }
}