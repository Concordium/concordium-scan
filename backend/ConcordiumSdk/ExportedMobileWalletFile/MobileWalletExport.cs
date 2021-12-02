using System.Linq;
using ConcordiumSdk.Types;

namespace ConcordiumSdk.ExportedMobileWalletFile;

public class MobileWalletExport
{
    public MobileWalletExport(Identity[] identities)
    {
        Identities = identities;
    }

    public Identity[] Identities { get; }

    public string GetSingleSignKeyForAccountWithAddress(AccountAddress address)
    {
        return Identities
            .SelectMany(identity => identity.Accounts
                .Where(account => account.Address == address)
                .Select(x => x.AccountKeys.Keys["0"].Keys["0"].SignKey)) // Assume single SignKey is found here
            .Single();
    }
}