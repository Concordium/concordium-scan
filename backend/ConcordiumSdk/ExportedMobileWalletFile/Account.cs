using ConcordiumSdk.Types;

namespace ConcordiumSdk.ExportedMobileWalletFile;

public class Account
{
    public Account(AccountAddress address, AccountKeysRootContainer accountKeys)
    {
        Address = address;
        AccountKeys = accountKeys;
    }

    public AccountAddress Address { get; }
    public AccountKeysRootContainer AccountKeys { get; }
}