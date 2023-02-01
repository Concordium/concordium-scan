using System.Formats.Cbor;

namespace Application.Api.GraphQL.Transactions;

public class DecodedText
{
    private DecodedText(string text, TextDecodeType decodeType)
    {
        Text = text;
        DecodeType = decodeType;
    }

    public static DecodedText CreateFromHex(string hex)
    {
        if (TryCborDecodeToText(hex, out var decodedText))
            return new DecodedText(decodedText!, TextDecodeType.Cbor);
        return new DecodedText(hex, TextDecodeType.Hex);
    }

    public string Text { get; }
    public TextDecodeType DecodeType { get; }

    static bool TryCborDecodeToText(string hex, out string? decodedText)
    {
        var bytes = Convert.FromHexString(hex);
        var encoder = new CborReader(bytes);
        try
        {
            var textRead = encoder.ReadTextString();
            if (encoder.BytesRemaining == 0)
            {
                decodedText = textRead;
                return true;
            }
        }
        catch (CborContentException) { }
        catch (InvalidOperationException) { }

        decodedText = null;
        return false;
    }
}

/// <summary>
/// The decoding type that was used to decode the binary data to readable text.
/// </summary>
public enum TextDecodeType
{
    /// <summary>
    /// Binary data has been decoded using CBOR text decoding (Concise Binary Object Representation)
    /// </summary>
    Cbor,
    /// <summary>
    /// Binary data has been hex encoded. This is the fallback if other decode types are not successful.
    /// </summary>
    Hex
}
