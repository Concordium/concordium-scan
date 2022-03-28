using ConcordiumSdk.Types;

namespace Tests.TestUtilities;

public class AccountAddressHelper
{
    public static string GetBaseAddress(string address)
    {
        return new AccountAddress(address).GetBaseAddress().AsString;
    }
}