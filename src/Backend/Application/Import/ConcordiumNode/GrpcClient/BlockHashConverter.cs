using System.Text.Json;
using System.Text.Json.Serialization;

namespace Application.Import.ConcordiumNode.GrpcClient;

public class BlockHashConverter : JsonConverter<BlockHash>
    {
        public override BlockHash Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var value = reader.GetString();
            if (value == null)
                throw new JsonException("BlockHash cannot be null.");
            return new BlockHash(value);
        }

        public override void Write(Utf8JsonWriter writer, BlockHash value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.AsString);
        }
    }