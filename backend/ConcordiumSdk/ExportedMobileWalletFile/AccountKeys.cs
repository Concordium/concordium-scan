namespace ConcordiumSdk.ExportedMobileWalletFile;

public class AccountKeys
{
    public AccountKeys(string signKey, string verifyKey)
    {
        SignKey = signKey;
        VerifyKey = verifyKey;
    }

    public string SignKey { get; }
    public string VerifyKey { get; }
}