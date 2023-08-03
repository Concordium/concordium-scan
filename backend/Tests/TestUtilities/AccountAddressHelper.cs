using System.Buffers.Binary;
using System.Threading;
using Concordium.Sdk.Types;

namespace Tests.TestUtilities;

public static class AccountAddressHelper
{
    private static readonly AccountAddress TemplateAddress = AccountAddress.From("3XSLuJcXg6xEua6iBPnWacc3iWh93yEDMCqX8FbE3RDSbEnT9P");
    private static UInt32 _nextUniqueValue = 0;

    public static AccountAddress CreateOneFilledWith(byte fill)
    {
        Span<byte> array = stackalloc byte[32];
        array.Fill(fill);
        return AccountAddress.From(array.ToArray());
    }

    public static string GetBaseAddress(string address)
    {
        return AccountAddress.From(address).GetBaseAddress().ToString();
    }

    public static string GetAliasAddress(string address, byte aliasByte1, byte aliasByte2 = 0, byte aliasByte3 = 0)
    {
        return AccountAddress.From(address).CreateAliasAddress(aliasByte1, aliasByte2, aliasByte3).ToString();
    }

    public static string GetUniqueAddress()
    {
        var bytes = TemplateAddress.ToBytes();
        var span = new Span<byte>(bytes);
        var value = Interlocked.Increment(ref _nextUniqueValue);
        BinaryPrimitives.WriteUInt32BigEndian(span.Slice(0, 4), value);
        return AccountAddress.From(bytes).ToString();
    }
}