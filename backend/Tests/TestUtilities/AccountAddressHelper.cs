using System.Buffers.Binary;
using System.Threading;
using ConcordiumSdk.Types;

namespace Tests.TestUtilities;

public static class AccountAddressHelper
{
    private static readonly AccountAddress TemplateAddress = new AccountAddress("3XSLuJcXg6xEua6iBPnWacc3iWh93yEDMCqX8FbE3RDSbEnT9P");
    private static UInt32 _nextUniqueValue = 0;

    public static string GetBaseAddress(string address)
    {
        return new AccountAddress(address).GetBaseAddress().AsString;
    }

    public static string GetAliasAddress(string address, byte aliasByte1, byte aliasByte2 = 0, byte aliasByte3 = 0)
    {
        return new AccountAddress(address).CreateAliasAddress(aliasByte1, aliasByte2, aliasByte3).AsString;
    }

    public static string GetUniqueAddress()
    {
        var bytes = TemplateAddress.AsBytes;
        var span = new Span<byte>(bytes);
        var value = Interlocked.Increment(ref _nextUniqueValue);
        BinaryPrimitives.WriteUInt32BigEndian(span.Slice(0, 4), value);
        return new AccountAddress(bytes).AsString;
    }
}