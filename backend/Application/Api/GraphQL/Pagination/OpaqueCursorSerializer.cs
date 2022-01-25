using System.Buffers;
using System.Buffers.Text;
using System.Text;

namespace Application.Api.GraphQL.Pagination;

public class OpaqueCursorSerializer : ICursorSerializer
{
    public string Serialize(long value)
    {
        var buffer = new byte[32];
        var span = (Span<byte>)buffer;
        if (!Utf8Formatter.TryFormat(value, span, out var written))
            throw new InvalidOperationException("Could not format value.");
        if (Base64.EncodeToUtf8InPlace(span, written, out written) != OperationStatus.Done)
            throw new InvalidOperationException("Could not encode value.");
        return Encoding.UTF8.GetString(buffer, 0, written);
    }

    public long Deserialize(string serializedValue)
    {
        var buffer = new byte[32];
        var span = (Span<byte>)buffer;
        var written = Encoding.UTF8.GetBytes(serializedValue, 0, serializedValue.Length, buffer, 0);
        if (Base64.DecodeFromUtf8InPlace(span.Slice(0, written),out written) != OperationStatus.Done)
            throw new InvalidOperationException("Could not decode value.");
        if (!Utf8Parser.TryParse(span.Slice(0, written), out long result, out _))
            throw new InvalidOperationException("Could not parse value.");
        return result;
    }
}