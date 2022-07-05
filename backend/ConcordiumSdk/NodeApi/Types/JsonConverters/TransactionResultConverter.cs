using System.Text.Json;
using System.Text.Json.Serialization;
using ConcordiumSdk.Utilities;

namespace ConcordiumSdk.NodeApi.Types.JsonConverters;

public class TransactionResultConverter : JsonConverter<TransactionResult>
{
    public override TransactionResult? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        reader.EnsureTokenType(JsonTokenType.StartObject);
        var startDepth = reader.CurrentDepth;
        var outcome = reader.ReadString("outcome");

        var result = outcome switch
        {
            "success" => ReadSuccessResult(ref reader, options),
            "reject" => ReadRejectResult(ref reader, options),
            _ => throw new NotImplementedException() 
        };

        reader.ForwardReaderToTokenTypeAtDepth(JsonTokenType.EndObject, startDepth);
        return result;
    }

    private TransactionSuccessResult ReadSuccessResult(ref Utf8JsonReader reader, JsonSerializerOptions options)
    {
        reader.ForwardReaderToPropertyValue("events");
        
        var events = JsonSerializer.Deserialize<TransactionResultEvent[]>(ref reader, options);
        if (events == null)
            throw new InvalidOperationException("events were null when trying to deserialize a successful outcome!");
        
        return new TransactionSuccessResult { Events = events };
    }

    private TransactionResult ReadRejectResult(ref Utf8JsonReader reader, JsonSerializerOptions options)
    {
        reader.ForwardReaderToPropertyValue("rejectReason");
        
        var rejectReason = JsonSerializer.Deserialize<TransactionRejectReason>(ref reader, options);
        if (rejectReason == null)
            throw new InvalidOperationException("reject reason were null when trying to deserialize a reject outcome!");
        
        return new TransactionRejectResult { Reason = rejectReason };
    }

    public override void Write(Utf8JsonWriter writer, TransactionResult value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}