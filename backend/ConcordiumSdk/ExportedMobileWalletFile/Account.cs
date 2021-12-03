using ConcordiumSdk.Types;

namespace ConcordiumSdk.ExportedMobileWalletFile;

public class Account
{
    public Account(AccountAddress address, AccountKeysRootContainer accountKeys, string name)
    {
        Address = address;
        AccountKeys = accountKeys;
        Name = name;
    }

    public AccountAddress Address { get; }
    public AccountKeysRootContainer AccountKeys { get; }
    public string Name { get; }
}