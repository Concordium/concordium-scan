using System.Collections.Generic;

namespace ConcordiumSdk.ExportedMobileWalletFile;

public class AccountKeysRootContainer
{
    public AccountKeysRootContainer(Dictionary<string, AccountKeysSubContainer> keys, int threshold)
    {
        Keys = keys;
        Threshold = threshold;
    }

    public Dictionary<string, AccountKeysSubContainer> Keys { get; }
    public int Threshold { get; }
}