namespace ConcordiumSdk.ExportedMobileWalletFile;

public class Identity
{
    public Identity(Account[] accounts)
    {
        Accounts = accounts;
    }

    public Account[] Accounts { get; }
}