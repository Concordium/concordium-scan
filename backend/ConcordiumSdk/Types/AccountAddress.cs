using System.Linq;
using NBitcoin.DataEncoders;

namespace ConcordiumSdk.Types;

public class AccountAddress : Address
{
    private static readonly Base58CheckEncoder EncoderInstance = new();
    private readonly byte[] _bytes;

    /// <summary>
    /// Creates an instance from a 32 byte address (ie. excluding the version byte).
    /// </summary>
    public AccountAddress(byte[] bytes)
    {
        _bytes = bytes ?? throw new ArgumentNullException(nameof(bytes));

        if (_bytes.Length != 32) throw new ArgumentException("Expected length to be exactly 32 bytes");
        
        var bytesToEncode = new byte[33];
        bytesToEncode[0] = 1;
        bytes.CopyTo(bytesToEncode, 1);
        AsString = EncoderInstance.EncodeData(bytesToEncode);
    }
    
    /// <summary>
    /// Creates an instance from a base58-check encoded string
    /// </summary>
    public AccountAddress(string base58CheckEncodedAddress)
    {
        AsString = base58CheckEncodedAddress ?? throw new ArgumentNullException(nameof(base58CheckEncodedAddress));

        var decodedBytes = EncoderInstance.DecodeData(base58CheckEncodedAddress);
        _bytes = decodedBytes.Skip(1).ToArray(); // Remove version byte
    }

    public static bool TryParse(string? base58CheckEncodedAddress, out AccountAddress? result)
    {
        result = null;
        if (base58CheckEncodedAddress == null) return false;
        
        try
        {
            result = new AccountAddress(base58CheckEncodedAddress);
            return true;
        }
        catch (FormatException)
        {
            // Decode throws FormatException if decode is not successful
            return false;
        }
    }

    public static bool IsValid(string? base58CheckEncodedAddress)
    {
        return TryParse(base58CheckEncodedAddress, out _);
    }

    /// <summary>
    /// Gets the address as a byte array (without leading version byte).
    /// Will always be 32 bytes. 
    /// </summary>
    public byte[] AsBytes => _bytes;

    /// <summary>
    /// Gets the address as a base58-check encoded string.
    /// </summary>
    public string AsString { get; }

    public override string ToString()
    {
        return AsString;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return AsString == ((AccountAddress)obj).AsString;
    }

    public override int GetHashCode()
    {
        return AsString.GetHashCode();
    }

    public static bool operator ==(AccountAddress? left, AccountAddress? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(AccountAddress? left, AccountAddress? right)
    {
        return !Equals(left, right);
    }

    public AccountAddress GetBaseAddress()
    {
        return CreateAliasAddress(0, 0, 0);
    }

    public AccountAddress CreateAliasAddress(byte byte1, byte byte2, byte byte3)
    {
        var aliasBytes = AsBytes.Take(29).Concat(new[] { byte1, byte2, byte3 }).ToArray();
        return new AccountAddress(aliasBytes);
    }
}