using System.Collections.Generic;

namespace ConcordiumSdk.ExportedMobileWalletFile;

public class AccountKeysSubContainer
{
    public AccountKeysSubContainer(Dictionary<string, AccountKeys> keys, int threshold)
    {
        Keys = keys;
        Threshold = threshold;
    }

    public Dictionary<string, AccountKeys> Keys { get; }
    public int Threshold { get; }
}