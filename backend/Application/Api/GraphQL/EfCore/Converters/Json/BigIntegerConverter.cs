using System.Globalization;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Application.Api.GraphQL.EfCore.Converters.Json;

/// <summary>
/// Mapping <see cref="BigInteger"/> to raw json number representation in database.
/// https://stackoverflow.com/questions/64788895/serialising-biginteger-using-system-text-json/65350863#65350863
/// </summary>
public class BigIntegerConverter : JsonConverter<BigInteger>
{
    public override BigInteger Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        
        if (reader.TokenType != JsonTokenType.Number)
        {
            throw new JsonException($"Found token {reader.TokenType} but expected token {JsonTokenType.Number}");
        }

        using var doc = JsonDocument.ParseValue(ref reader);
        return BigInteger.Parse(doc.RootElement.GetRawText(), NumberFormatInfo.InvariantInfo);
    }

    public override void Write(Utf8JsonWriter writer, BigInteger value, JsonSerializerOptions options)
    {
        writer.WriteRawValue(value.ToString(NumberFormatInfo.InvariantInfo));
    }
}
