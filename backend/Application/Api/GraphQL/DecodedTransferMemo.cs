using ConcordiumSdk.Types;

namespace Application.Api.GraphQL;

public class DecodedTransferMemo
{
    private DecodedTransferMemo(string text, TextDecodeType decodeType)
    {
        Text = text;
        DecodeType = decodeType;
    }

    public static DecodedTransferMemo CreateFromHex(string hex)
    {
        var memo = Memo.CreateFromHex(hex);
        if (memo.TryCborDecodeToText(out var decodedText))
            return new DecodedTransferMemo(decodedText!, TextDecodeType.Cbor);
        return new DecodedTransferMemo(hex, TextDecodeType.Hex);
    }
    
    public string Text { get; }
    public TextDecodeType DecodeType { get; }
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
